namespace Franken;

/// <summary>
/// 身体部件数据，基础能力值与<see cref="ActorStats"/>一一对应
/// <br/>合成时，int直接增加基础数值，float比例提升基础数值
/// <br/>还有名称、词条、技能tag
/// </summary>
public class ActorBodyPartStats
{
    public Number Hp { get; set; }
    public Number San { get; set; }
    public Number Mp { get; set; }
    public Number Pt { get; set; }
    public Number Atk { get; set; }
    public Number Def { get; set; }
    public Number Agi { get; set; }

    public static ActorBodyPartStats FromCSV(string name)
    {
        var part = CSV.ActorBodyPart.Get(name);
        if (part == null) return null;

        return new()
        {
            Hp = new(part.Hp),
            San = new(part.San),
            Mp = new(part.Mp),
            Pt = new(part.Pt),
            Atk = new(part.Atk),
            Def = new(part.Def),
            Agi = new(part.Agi)
        };
    }
}