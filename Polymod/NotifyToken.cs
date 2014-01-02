using Polymod.Fluent;
using System;
using System.Collections.Generic;

namespace Polymod
{
    public static class NotifyScope
    {
        public static INotifyScope Create()
        {
            return new NotifyToken();
        }
    }

    public interface INotifyScope : IDisposable
    {
        void NotifyChanged(IRaisePropertyChanged source, string propertyName);
    }

    internal class NotifyToken : INotifyScope
    {
        class NotifyTokenInfo
        {
            public Dictionary<IRaisePropertyChanged, HashSet<string>> Notifications { get; private set; }
            public object Synclock { get; private set; }
            public int ReferenceCounter { get; set; }
            public NotifyTokenInfo()
            {
                Synclock = new object();
                Notifications = new Dictionary<IRaisePropertyChanged, HashSet<string>>();
                ReferenceCounter = 0;
            }
        }

        [ThreadStatic]
        private static readonly NotifyTokenInfo Info = new NotifyTokenInfo();

        public NotifyToken()
        {
            lock (Info.Synclock)
            {
                Info.ReferenceCounter++;
            }
        }

        public void Dispose()
        {
            Dictionary<IRaisePropertyChanged, HashSet<string>> notifications = null;

            lock (Info.Synclock)
            {
                Info.ReferenceCounter--;
                if (Info.ReferenceCounter == 0)
                {
                    //Create a local copy...
                    notifications = new Dictionary<IRaisePropertyChanged, HashSet<string>>(Info.Notifications);
                    Info.Notifications.Clear();
                }
            }

            if (notifications != null)
            {
                foreach (var kp in notifications)
                {
                    foreach (var propertyName in kp.Value)
                    {
                        kp.Key.RaisePropertyChanged(propertyName);
                    }
                }
            }
        }

        public void NotifyChanged(IRaisePropertyChanged source, string propertyName)
        {
            if (source == null) throw new ArgumentNullException("source");
            
            lock (Info.Synclock)
            {
                HashSet<string> tmp;
                if (Info.Notifications.TryGetValue(source, out tmp)) tmp.Add(propertyName);
                else Info.Notifications.Add(source, new HashSet<string>(new[] { propertyName }));
            }
        }
    }
}
