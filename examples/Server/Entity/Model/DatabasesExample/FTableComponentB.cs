using Fantasy.Database.Attributes;
using Fantasy.Entitas;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fantasy
{
    //[FTable("ExampleComponentB", Description = "Fantasy Table Test Component")]
    public class FTableComponentB : Entity
    {
        public int TestIntField;

        public int TestStringField;

        [NotMapped]
        public int NotMapped;
    }
}
