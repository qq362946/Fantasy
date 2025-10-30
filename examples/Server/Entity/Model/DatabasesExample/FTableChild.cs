using Fantasy.Database.Attributes;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fantasy
{
    //[FTable("ExampleComponent", Description = "Fantasy Table Test Child")]
    public class FTableChild :Entity, ISupportedMultiEntity
    {
        public int TestIntField;

        public int TestStringField;

        [NotMapped]
        public int NotMapped;
    }
}
