using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace L1
{
    class Person
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; }
        public string endPoint { get; set; }
        public List<string> Messages { get; set; }
    }
}
