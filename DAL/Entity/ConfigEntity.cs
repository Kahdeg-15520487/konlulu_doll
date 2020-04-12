using System;
using System.Collections.Generic;
using System.Text;

namespace konlulu.DAL.Entity
{
    public class ConfigEntity : BaseEntity
    {
        public ConfigEntity(string configName, int configValue)
        {
            this.ConfigName = configName;
            this.ConfigValue = configValue;
        }

        public string ConfigName { get; set; }
        public int ConfigValue { get; set; }

        public override string ToString()
        {
            return $"{ConfigName}:{ConfigValue}";
        }
    }
}
