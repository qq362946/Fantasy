using Fantasy.Database.Attributes;
using Fantasy.Entitas;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fantasy
{
    //[FantasyDbSet("ExampleComponentA", Description = "Fantasy Table Test Component")]
    public class FantasyDbSetComponentA : Entity
    {
        public int TestIntField;

        public int TestStringField;

        [NotMapped]
        public int NotMapped;
    }
}
