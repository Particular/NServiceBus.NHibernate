namespace NServiceBus.SagaPersisters.NHibernate.AutoPersistence
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using global::NHibernate;
    using global::NHibernate.Cfg.MappingSchema;
    using global::NHibernate.Mapping.ByCode;
    using global::NHibernate.Type;
    using NServiceBus.Sagas;

    class SagaModelMapper
    {
        readonly Func<Type, string> tableNamingConvention;

        public SagaModelMapper(SagaMetadataCollection allMetadata, IEnumerable<Type> typesToScan)
            : this(allMetadata, typesToScan, DefaultTableNameConvention)
        {
            
        }

        public SagaModelMapper(SagaMetadataCollection allMetadata, IEnumerable<Type> typesToScan, Func<Type, string> tableNamingConvention)
        {
            this.tableNamingConvention = tableNamingConvention;
            Mapper = new ConventionModelMapper();

            this.typesToScan = typesToScan.ToList();

            this.sagaMetaModel = allMetadata;

            sagaEntities =
                this.typesToScan.Where(t => typeof(IContainSagaData).IsAssignableFrom(t) && !t.IsInterface).ToList();

            PopulateTypesThatShouldBeAutoMapped();

            Mapper.IsTablePerClass((type, b) => false);
            Mapper.IsTablePerConcreteClass((type, b) => sagaEntities.Contains(type));
            Mapper.IsTablePerClassHierarchy((type, b) => false);
            Mapper.IsEntity((type, mapped) => entityTypes.ContainsKey(type));
            Mapper.IsArray((info, b) => false);
            Mapper.IsBag((info, b) =>
            {
                var memberType = info.GetPropertyOrFieldType();
                return typeof(IEnumerable).IsAssignableFrom(memberType) &&
                       !(memberType == typeof(string) || memberType == typeof(byte[]) || memberType.IsArray);
            });
            Mapper.IsPersistentProperty((info, b) => !HasAttribute<RowVersionAttribute>(info));
            Mapper.BeforeMapClass += ApplyClassConvention;
            Mapper.BeforeMapUnionSubclass += ApplySubClassConvention;
            Mapper.BeforeMapProperty += ApplyPropertyConvention;
            Mapper.BeforeMapBag += ApplyBagConvention;
            Mapper.BeforeMapManyToOne += ApplyManyToOneConvention;
        }

        public ConventionModelMapper Mapper { get; private set; }

        void ApplyClassConvention(IModelInspector mi, Type type, IClassAttributesMapper map)
        {
            if (!sagaEntities.Contains(type))
            {
                map.Id(idMapper => idMapper.Generator(Generators.GuidComb));
            }
            else
            {
                map.Id(idMapper => idMapper.Generator(Generators.Assigned));
            }

            var rowVersionProperty = type.GetProperties()
                .Where(HasAttribute<RowVersionAttribute>)
                .FirstOrDefault();

            if (rowVersionProperty != null)
            {
                map.Version(rowVersionProperty, mapper =>
                {
                    mapper.Generated(VersionGeneration.Never);

                    if (rowVersionProperty.PropertyType == typeof(DateTime))
                    {
                        mapper.Type(new TimestampType());
                    }

                    if (rowVersionProperty.PropertyType == typeof(byte[]))
                    {
                        mapper.Type(new BinaryBlobType());
                        mapper.Generated(VersionGeneration.Always);
                        mapper.UnsavedValue(null);
                        mapper.Column(cm =>
                        {
                            cm.NotNullable(false);
                            cm.SqlType(NHibernateUtil.Timestamp.Name);
                        });
                    }
                });
            }

            var tableAttribute = GetAttribute<TableNameAttribute>(type);

            if (tableAttribute != null)
            {
                map.Table(tableAttribute.TableName);
                if (!String.IsNullOrEmpty(tableAttribute.Schema))
                {
                    map.Schema(tableAttribute.Schema);
                }

                return;
            }

            var namingConvention = tableNamingConvention(type);
            map.Table(namingConvention);
        }

        static string DefaultTableNameConvention(Type type)
        {
            //if the type is nested use the name of the parent
            if (type.DeclaringType == null)
            {
                return type.Name;
            }

            if (typeof(IContainSagaData).IsAssignableFrom(type))
            {
                return type.DeclaringType.Name;
            }

            return type.DeclaringType.Name + "_" + type.Name;
        }

        void ApplySubClassConvention(IModelInspector mi, Type type, IUnionSubclassAttributesMapper map)
        {
            var tableAttribute = GetAttribute<TableNameAttribute>(type);
            if (tableAttribute != null)
            {
                map.Table(tableAttribute.TableName);
                if (!String.IsNullOrEmpty(tableAttribute.Schema))
                {
                    map.Schema(tableAttribute.Schema);
                }

                return;
            }

            var namingConvention = tableNamingConvention(type);
            map.Table(namingConvention);
        }

        void ApplyPropertyConvention(IModelInspector mi, PropertyPath type, IPropertyMapper map)
        {
            if (type.PreviousPath != null)
            {
                if (mi.IsComponent(((PropertyInfo) type.PreviousPath.LocalMember).PropertyType))
                {
                    map.Column(type.PreviousPath.LocalMember.Name + type.LocalMember.Name);
                }
            }

            SagaMetadata sagaMetadata;
            if (type.LocalMember.DeclaringType != null && entityTypes.TryGetValue(type.LocalMember.DeclaringType, out sagaMetadata))
            {
                SagaMetadata.CorrelationPropertyMetadata correlationProperty;
                if (sagaMetadata.TryGetCorrelationProperty(out correlationProperty) && correlationProperty.Name == type.LocalMember.Name)
                {
                    map.Unique(true);
                }
            }

            var propertyInfo = type.LocalMember as PropertyInfo;
            if (propertyInfo != null)
            {
                if (propertyInfo.PropertyType == typeof(byte[]))
                {
                    map.Length(Int32.MaxValue);
                }

                return;
            }

            var fieldInfo = type.LocalMember as FieldInfo;
            if (fieldInfo != null && fieldInfo.FieldType == typeof(byte[]))
            {
                map.Length(Int32.MaxValue);
            }
        }

        void ApplyBagConvention(IModelInspector mi, PropertyPath type, IBagPropertiesMapper map)
        {
            map.Cascade(Cascade.All | Cascade.DeleteOrphans);
            map.Key(km => km.Column(type.LocalMember.DeclaringType.Name + "_id"));

            var bagType = type.LocalMember.GetPropertyOrFieldType().DetermineCollectionElementType();
            var parentType = type.LocalMember.DeclaringType;

            if (bagType.HasPublicPropertyOf(parentType))
            {
                map.Inverse(true);
            }
        }

        void ApplyManyToOneConvention(IModelInspector mi, PropertyPath type, IManyToOneMapper map)
        {
            map.Column(type.LocalMember.Name + "_id");
        }

        public HbmMapping Compile()
        {
            var hbmMapping = Mapper.CompileMappingFor(entityTypes.Keys);

            ApplyOptimisticLockingOnMapping(hbmMapping);

            return hbmMapping;
        }

        static void ApplyOptimisticLockingOnMapping(HbmMapping hbmMapping)
        {
            foreach (var rootClass in hbmMapping.RootClasses)
            {
                if (rootClass.Version != null)
                {
                    continue;
                }

                rootClass.dynamicupdate = true;
                rootClass.optimisticlock = HbmOptimisticLockMode.All;
            }

            foreach (var hbmSubclass in hbmMapping.UnionSubclasses)
            {
                hbmSubclass.dynamicupdate = true;
            }

            foreach (var hbmSubclass in hbmMapping.JoinedSubclasses)
            {
                hbmSubclass.dynamicupdate = true;
            }

            foreach (var hbmSubclass in hbmMapping.SubClasses)
            {
                hbmSubclass.dynamicupdate = true;
            }
        }

        void PopulateTypesThatShouldBeAutoMapped()
        {
            foreach (var sagaMetadata in sagaMetaModel)
            {
                if (typesToScan.Contains(sagaMetadata.SagaEntityType))
                {
                    AddEntitiesToBeMapped(sagaMetadata, sagaMetadata.SagaEntityType);
                }
            }
        }

        void AddEntitiesToBeMapped(SagaMetadata sagaMetadata, Type rootEntity)
        {
            if (entityTypes.ContainsKey(rootEntity))
            {
                return;
            }

            entityTypes.Add(rootEntity, sagaMetadata);

            var propertyInfos = rootEntity.GetProperties();
                
            foreach (var propertyInfo in propertyInfos)
            {
                if (propertyInfo.PropertyType.GetProperty("Id") != null)
                {
                    AddEntitiesToBeMapped(sagaMetadata, propertyInfo.PropertyType);
                }

                if (propertyInfo.PropertyType.IsGenericType)
                {
                    var args = propertyInfo.PropertyType.GetGenericArguments();

                    if (args[0].GetProperty("Id") != null)
                    {
                        AddEntitiesToBeMapped(sagaMetadata, args[0]);
                    }
                }

                if (rootEntity.BaseType != typeof(object) && HasAttribute<RowVersionAttribute>(propertyInfo))
                {
                    throw new MappingException(string.Format("RowVersionAttribute is not supported on derived classes, please remove RowVersionAttribute from '{0}' or derive directly from IContainSagaData", rootEntity));
                }
            }

            var derivedTypes = typesToScan.Where(t => t.IsSubclassOf(rootEntity));

            foreach (var derivedType in derivedTypes)
            {
                AddEntitiesToBeMapped(sagaMetadata, derivedType);
            }

            var superClasses = typesToScan.Where(t => t.IsAssignableFrom(rootEntity));

            foreach (var superClass in superClasses)
            {
                AddEntitiesToBeMapped(sagaMetadata, superClass);
            }
        }

        static T GetAttribute<T>(Type type) where T : Attribute
        {
            var attributes = type.GetCustomAttributes(typeof(T), false);
            return attributes.FirstOrDefault() as T;
        }

        static bool HasAttribute<T>(MemberInfo mi) where T : Attribute
        {
            var attributes = mi.GetCustomAttributes(typeof(T), false);
            return attributes.Any();
        }

        Dictionary<Type, SagaMetadata> entityTypes = new Dictionary<Type, SagaMetadata>();
        readonly List<Type> sagaEntities;
        readonly List<Type> typesToScan;
        readonly SagaMetadataCollection sagaMetaModel;
    }
}