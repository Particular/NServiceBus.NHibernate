namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    class CallbackList
    {
        Func<Task> singleItem;
        List<Func<Task>> multipleItems;

        public void Add(Func<Task> callback)
        {
            if (multipleItems != null)
            {
                multipleItems.Add(callback);
            }
            else if (singleItem != null)
            {
                multipleItems = new List<Func<Task>>
                {
                    singleItem,
                    callback
                };
                singleItem = null;
            }
            else
            {
                singleItem = callback;
            }
        }

        public async Task InvokeAll()
        {
            if (singleItem != null)
            {
                await singleItem().ConfigureAwait(false);
            }
            else if (multipleItems != null)
            {
                foreach (var callback in multipleItems)
                {
                    await callback().ConfigureAwait(false);
                }
            }
        }
    }
}