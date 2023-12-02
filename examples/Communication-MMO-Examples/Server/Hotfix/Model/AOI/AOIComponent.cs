
namespace Fantasy;

public class AOIComponent : Entity
{
    public const int CellUnitLen = 10000;
    public const int CellSize = 10 * CellUnitLen;
    public Dictionary<long, Cell> Cells = new Dictionary<long, Cell>();
}