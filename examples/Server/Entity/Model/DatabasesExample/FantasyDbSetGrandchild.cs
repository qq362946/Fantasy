using Fantasy.Database.Attributes;
using Fantasy.Entitas;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fantasy
{
    //[FantasyDbSet("ExampleGrandchild", Description = "Fantasy Table Test Grandchild")]
    public class FantasyDbSetGrandchild : Entity
    {
        public int TestIntField;

        public int TestStringField;

        [NotMapped]
        public int NotMapped;
    }
}