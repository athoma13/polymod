using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polymod
{
    public class NotificationRegister
    {
        private readonly Dictionary<Type, Dictionary<string, List<string>>> _data = new Dictionary<Type, Dictionary<string, List<string>>>();

        public void AddRegistration(Type type, string sourcePropertyName, string affectedPropertyName)
        {
            if (sourcePropertyName == null) throw new ArgumentNullException("sourcePropertyName");
            if (type == null) throw new ArgumentNullException("type");

            Dictionary<string, List<string>> tmp;
            if (!_data.TryGetValue(type, out tmp))
            {
                tmp = new Dictionary<string, List<string>>();
                _data.Add(type, tmp);
            }

            List<string> tmp2;
            if (!tmp.TryGetValue(sourcePropertyName, out tmp2))
            {
                tmp2 = new List<string>();
                tmp.Add(sourcePropertyName, tmp2);
            }

            if (!tmp2.Contains(affectedPropertyName)) tmp2.Add(affectedPropertyName);
        }

        public string[] GetAffectedProperties(Type type, string sourcePropertyName, bool includeSelf = true)
        {
            var affectedProperties = new HashSet<string>();
            BuildAffectedProperties(type, sourcePropertyName, affectedProperties);
            if (includeSelf) affectedProperties.Add(sourcePropertyName);

            return affectedProperties.ToArray();
        }

        private void BuildAffectedProperties(Type type, string sourcePropertyName, HashSet<string> affectedProperties)
        {
            if (affectedProperties.Contains(sourcePropertyName)) return;

            Dictionary<string, List<string>> tmp;
            if (!_data.TryGetValue(type, out tmp)) return;

            List<string> tmp2;
            if (!tmp.TryGetValue(sourcePropertyName, out tmp2)) return;

            foreach (var affectedProperty in tmp2)
            {
                BuildAffectedProperties(type, affectedProperty, affectedProperties);
                affectedProperties.Add(affectedProperty);
            }
        }
    }
}
