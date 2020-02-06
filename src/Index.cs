using System;
using System.Collections.Generic;
using System.Text;

namespace Postgres.SchemaUpdater
{
    public class Index
    {
        public Index(string name, string expression)
        {
            Name = name;
            Expression = expression;
        }

        public string Name { get; }
        public string Expression { get; }
    }
}
