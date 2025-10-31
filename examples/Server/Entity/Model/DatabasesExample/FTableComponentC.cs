using Fantasy.Database.Attributes;
using Fantasy.Entitas;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fantasy
{
    //[FTable("ExampleComponentC", Description = "Fantasy Table Test Component")]
    public class FTableComponentC : Entity
    {
        public int TestIntField;

        public int TestStringField;

        [NotMapped]
        public int NotMapped;
    }
}
