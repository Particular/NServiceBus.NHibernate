namespace NServiceBus.SagaPersisters.NHibernate.AutoPersistence
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using global::NHibernate;
    using global::NHibernate.Cfg;
    using global::NHibernate.Cfg.MappingSchema;
    using global::NHibernate.Mapping;
    using global::NHibernate.Mapping.ByCode;
    using global::NHibernate.Type;
    using Sagas;

    class SagaModelMapper
    {
        SagaModelMapper(SagaMetadataCollection allMetadata, IEnumerable<Type> typesToScan, Func<Type, string> tableNamingConvention = null)
        {
            this.tableNamingConvention = tableNamingConvention ?? DefaultTableNameConvention;
            mapper = new ConventionModelMapper();

            this.typesToScan = typesToScan.ToList();

            sagaMetaModel = allMetadata;

            sagaEntities =
                this.typesToScan.Where(t => typeof(IContainSagaData).IsAssignableFrom(t) && !t.IsInterface).ToList();

            PopulateTypesThatShouldBeAutoMapped();

            mapper.IsTablePerClass((type, b) => false);
            mapper.IsTablePerConcreteClass((type, b) => sagaEntities.Contains(type));
            mapper.IsTablePerClassHierarchy((type, b) => false);
            mapper.IsEntity((type, mapped) => entityTypes.Contains(type));
            mapper.IsArray((info, b) => false);
            mapper.IsBag((info, b) =>
            {
                var memberType = info.GetPropertyOrFieldType();
                return typeof(IEnumerable).IsAssignableFrom(memberType) &&
                       !(memberType == typeof(string) || memberType == typeof(byte[]) || memberType.IsArray);
            });
            mapper.IsPersistentProperty((info, b) => !HasAttribute<RowVersionAttribute>(info));
            mapper.BeforeMapClass += ApplyClassConvention;
            mapper.BeforeMapUnionSubclass += ApplySubClassConvention;
            mapper.BeforeMapProperty += ApplyPropertyConvention;
            mapper.BeforeMapBag += ApplyBagConvention;
            mapper.BeforeMapManyToOne += ApplyManyToOneConvention;
        }

        public static List<string> AddMappings(Configuration configuration, SagaMetadataCollection allSagaMetadata, IEnumerable<Type> types, Func<Type, string> tableNamingConvention = null)
        {
            var modelMapper = new SagaModelMapper(allSagaMetadata, types, tableNamingConvention);
            var conventionMapping = modelMapper.Compile();
            configuration.AddMapping(conventionMapping);
            configuration.BuildMappings();
            var mappings = configuration.CreateMappings();

            foreach (var collection in mappings.IterateCollections)
            {
                var table = collection.CollectionTable;

                foreach (var foreignKey in table.ForeignKeyIterator)
                {
                    var idx = new Index();
                    idx.AddColumns(foreignKey.ColumnIterator);
                    idx.Name = "IDX" + foreignKey.Name.Substring(2);
                    idx.Table = table;
                    table.AddIndex(idx);
                }
            }

            return conventionMapping.RootClasses.Select(x => x.Name).ToList();
        }

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
                        mapper.Type(new DateTimeType());
                    }

                    if (rowVersionProperty.PropertyType == typeof(byte[]))
                    {
                        mapper.Type(new BinaryBlobType());
                        mapper.Generated(VersionGeneration.Always);
                        mapper.UnsavedValue(null);
                        mapper.Column(cm =>
                        {
                            cm.NotNullable(false);
                            cm.SqlType(NHibernateUtil.DateTime.Name);
                        });
                    }
                });
            }

            var tableAttribute = GetAttribute<TableNameAttribute>(type);

            if (tableAttribute != null)
            {
                map.Table(tableAttribute.TableName);
                if (!string.IsNullOrEmpty(tableAttribute.Schema))
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
                if (!string.IsNullOrEmpty(tableAttribute.Schema))
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

            if (type.LocalMember.DeclaringType != null)
            {
                var sagaMetadata = sagaMetaModel.FirstOrDefault(sm => sm.SagaEntityType == type.LocalMember.DeclaringType);

                if (sagaMetadata != null)
                {
                    SagaMetadata.CorrelationPropertyMetadata correlationProperty;
                    if (sagaMetadata.TryGetCorrelationProperty(out correlationProperty) && correlationProperty.Name == type.LocalMember.Name)
                    {
                        map.Unique(true);
                    }
                }
            }

            var propertyInfo = type.LocalMember as PropertyInfo;
            if (propertyInfo != null)
            {
                if (propertyInfo.PropertyType == typeof(byte[]))
                {
                    map.Length(int.MaxValue);
                }

                return;
            }

            var fieldInfo = type.LocalMember as FieldInfo;
            if (fieldInfo != null && fieldInfo.FieldType == typeof(byte[]))
            {
                map.Length(int.MaxValue);
            }
        }

        void ApplyBagConvention(IModelInspector mi, PropertyPath type, IBagPropertiesMapper map)
        {
            map.Cascade(Cascade.All | Cascade.DeleteOrphans);
            map.Key(km => km.Column(type.LocalMember.DeclaringType.Name + "_id"));

            var bagType = type.LocalMember.GetPropertyOrFieldType().DetermineCollectionElementType();
            var parentType = type.LocalMember.DeclaringType;
            childTables.Add(bagType);

            if (bagType.HasPublicPropertyOf(parentType))
            {
                map.Inverse(true);
            }
        }

        static void ApplyManyToOneConvention(IModelInspector mi, PropertyPath type, IManyToOneMapper map)
        {
            map.Column(type.LocalMember.Name + "_id");
        }

        HbmMapping Compile()
        {
            var hbmMapping = mapper.CompileMappingFor(entityTypes);

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
                    AddEntitiesToBeMapped(sagaMetadata.SagaEntityType);
                }
            }
        }

        void AddEntitiesToBeMapped(Type rootEntity)
        {
            if (entityTypes.Contains(rootEntity))
            {
                return;
            }
            if (rootEntity.IsAbstract && rootEntity != typeof(ContainSagaData))
            {
                return; //We skip user abstract classes
            }

            if (rootEntity == typeof(object))
            {
                return; //skip object as that will result in mapping all its derivatives
            }
            entityTypes.Add(rootEntity);

            var propertyInfos = rootEntity.GetProperties();

            foreach (var propertyInfo in propertyInfos)
            {
                if (propertyInfo.PropertyType.GetProperty("Id") != null)
                {
                    AddEntitiesToBeMapped(propertyInfo.PropertyType);
                }

                if (propertyInfo.PropertyType.IsGenericType)
                {
                    var args = propertyInfo.PropertyType.GetGenericArguments();

                    if (args[0].GetProperty("Id") != null)
                    {
                        AddEntitiesToBeMapped(args[0]);
                    }
                }

                if (rootEntity.BaseType != typeof(object) && HasAttribute<RowVersionAttribute>(propertyInfo))
                {
                    throw new MappingException($"RowVersionAttribute is not supported on derived classes, please remove RowVersionAttribute from '{rootEntity}' or derive directly from IContainSagaData");
                }
            }

            var derivedTypes = typesToScan.Where(t => t.IsSubclassOf(rootEntity));

            foreach (var derivedType in derivedTypes)
            {
                AddEntitiesToBeMapped(derivedType);
            }

            var superClasses = typesToScan.Where(t => t.IsAssignableFrom(rootEntity));

            foreach (var superClass in superClasses)
            {
                AddEntitiesToBeMapped(superClass);
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

        List<Type> sagaEntities;
        SagaMetadataCollection sagaMetaModel;
        Func<Type, string> tableNamingConvention;
        List<Type> typesToScan;
        List<Type> childTables = new List<Type>();
		List<Type> entityTypes = new List<Type>();
        ConventionModelMapper mapper;
    }
}
