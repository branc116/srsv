
namespace Lab3 {

public class Lab3 {
    public static async Task Main(string[] args) {
        var settings = new Settings(new List<(LiftSettings settings, LiftState state)> {
            (new LiftSettings(10, 20, 5), new LiftState(5, 1, 0, 0, LiftEnum.Moving, new List<Human> {
                new Human('b', 3, 5, 10),
                new Human('c', 0, 5, 11),
            }, new HashSet<int>(new [] {10, 11, 4}))),
            (new LiftSettings(5, 20, 5), new LiftState(1, -1, 15, 0, LiftEnum.Moving, new List<Human> {
                new Human('d', 7, 1, 0),
                new Human('e', 6, 1, 1),
            }, new HashSet<int>(new [] {0, 1, 10})))
        }, 15, 0.1, 13, new List<Human> {
            new Human('a', 4, 4, 0),
            new Human('f', 10, 10, 11)
        });
        while(true) {
            Print(settings);
            System.Console.WriteLine(settings.LiftSettings[0]);
            System.Console.WriteLine(settings.LiftSettings[0].state.CalledOnFloors.Any() ?
             settings.LiftSettings[0].state.CalledOnFloors.Select(f => f.ToString())
                .Aggregate((f1, f2) => $"{f1} {f2}") : "---");
            settings = settings.Tick();
            await Task.Delay(settings.MsPerTick);
        }
    }
    static Settings RandomEmptyState(int floors, int nLifts ) {
        throw new NotImplementedException();
    }
    static  void Print(Settings s) {
        string str = "";
        for(int i= s.Floors; i >= 0; --i) {
            str += "|";
            for(int j = 0; j < s.LiftSettings.Count; ++j) {
                if (s.LiftSettings[j].state.LastFloor == i) {
                    str += "+" + new string('-', s.LiftSettings[j].settings.Cappacity) + "+";
                }else {
                    str += new string(' ', s.LiftSettings[j].settings.Cappacity + 2);
                }
                str += '|';
            }
            str += "\n|";
            for(int j = 0; j < s.LiftSettings.Count; ++j) {
                if (s.LiftSettings[j].state.LastFloor == i) {
                    var humans = new string(s.LiftSettings[j].state.HumansInLift.Select(h => h.Name).ToArray());
                    var emptySpace = s.LiftSettings[j].settings.Cappacity - humans.Length; 
                    str += "|" + humans + new string(' ', emptySpace) + "|";
                }else {
                    str += new string(' ', s.LiftSettings[j].settings.Cappacity + 2);
                }
                str += '|';
                
            }
            str += $"{i}" + (s.HumansWaiting.Where(h => h.CurrentFloor == i).Any() ? '-' + s.HumansWaiting
                .Where(h => h.CurrentFloor == i)
                .Select(h =>{
                    var ticksToLift =s.LiftSettings.Select(ls => ls.TicksToGetToFloor(h.OriginFloor).ToString()).Aggregate((prev, nex) => $"{prev}, {nex}");
                     return $"{h.Name}({ticksToLift})";
                }).Aggregate((aaa, bbb) => $"{aaa};{bbb}") : "");
            str += "\n|";
            for(int j = 0; j < s.LiftSettings.Count; ++j) {
                if (s.LiftSettings[j].state.LastFloor == i) {
                    str += "+" + new string('-', s.LiftSettings[j].settings.Cappacity) + "+";
                }else {
                    str += new string(' ', s.LiftSettings[j].settings.Cappacity + 2);
                }
                str += '|';
            }
            str += '\n';
        }
        System.Console.Clear();
        System.Console.WriteLine(str);
    }
}
public record Settings(
    List<(LiftSettings settings, LiftState state)> LiftSettings,
    int MsPerTick,
    double SpawnFrequency,
    int Floors,
    List<Human> HumansWaiting
);
public record LiftSettings(
    int Cappacity,
    int TicksPerFloor,
    int TicksSpentOnFloor
);
public record LiftState(
    int LastFloor,
    int Direction, // -1 down, 1 up, 0 still
    int TravellingTime,
    int TimeOpen,
    LiftEnum State,
    List<Human> HumansInLift,
    HashSet<int> CalledOnFloors
);
public enum LiftEnum {Holding, Moving}
public record Human(
    char Name,
    int OriginFloor,
    int CurrentFloor,
    int DestinationFloor
) {
    public int Direction => DestinationFloor > OriginFloor ? 1 : -1;
}

public static class Help {
    public static int TicksToGetToFloor(this (LiftSettings settings, LiftState state) s, int floor) {
        var ((_, tpf, tsof), (lf, dir, tt, to, _, hil, cof)) = s;
        if (lf == floor && to < tsof) return 0;
        if (lf == floor && dir == 0) return 0;
        if (dir == 0) {
            var sum = Math.Sign(floor - lf) * floor - lf;
            return sum;
        }
        if (lf < floor && dir == 1) {
            var sum = tpf - tt + 
                      cof.Count(f => f > lf && f < floor) * tsof + 
                      (floor - lf - 1) * tpf;
            return sum;
        }
        if (lf <= floor && dir == -1) {
            var sum = tpf - tt +
                      cof.Count(f => f < lf) * tsof +
                      (lf - cof.MinOrDefault(lf + 1) - 1) * tpf +
                      (floor - cof.MinOrDefault(lf)) * tpf +
                      cof.Count(f => f > lf && f < floor) * tsof;
            return sum;
        }
        if (lf > floor && dir == -1) {
            var sum = tpf - tt + 
                      cof.Count(f => f < lf && f > floor) * tsof + 
                      (lf - floor - 1) * tpf;
            return sum;
        }
        if (lf >= floor && dir == 1) {
            var sum = tpf - tt +
                      cof.Count(f => f > lf) * tsof +
                      (cof.MaxOrDefault(lf  - 1) - lf  - 1) * tpf +
                      (cof.MaxOrDefault(floor) - floor) * tpf +
                      cof.Count(f => f < lf && f > floor) * tsof;
            return sum;
        }
        throw new NotImplementedException($"floor: {floor}, {s}");
    }
    public static Settings Tick(this Settings s) {
        var newS = s with {
            LiftSettings = s.LiftSettings.Select(lift => lift.Tick()).ToList()
        };
        HumansEnter(newS);
        return newS;
    }
    public static void HumansEnter(Settings s) {
        var notTravelling = s.LiftSettings.Where(l => l.state.TravellingTime == 0);
        if (!notTravelling.Any()) return;
        if (!s.HumansWaiting.Any()) return;
        var join = notTravelling.Join(s.HumansWaiting, l => l.state.LastFloor, h => h.CurrentFloor, (l, h) => (l, h)).ToList();
        foreach(var (l, h) in join) {
            if (l.state.Direction == h.Direction || l.state.Direction == 0) {
                s.HumansWaiting.Remove(h);
                l.state.HumansInLift.Add(h);
                l.state.CalledOnFloors.Add(h.DestinationFloor);
            }
        }
    }
    public static (LiftSettings, LiftState) Tick(this (LiftSettings set, LiftState st) s) {
        var ((_, tpf, tsof), (lf, dir, tt, to, le, hil, cof)) = s;
        return s with {
            st = s.st with {
                Direction = tt > 0 ? dir :
                    cof.Any(f => (lf - f) * dir <= -1 ) ? dir :
                    cof.Any(f => f != lf) ?
                    (((cof.Where(f => f > lf).MinOrDefault() - lf) < (lf - cof.Where(f => f < lf).MaxOrDefault())) ? 1 : -1) : 0,
                TravellingTime = le == LiftEnum.Moving ? tt + 1 : 0,
                HumansInLift = tt > 0 ? hil : hil.Where(h => h.DestinationFloor != lf).ToList(),
                TimeOpen = le == LiftEnum.Holding ? (to + 1) : 0,
                LastFloor = le == LiftEnum.Moving && tt + 1 == tpf ? lf + dir : lf,
                CalledOnFloors = tt == 0 ? cof : cof.Where(f => f != lf).ToHashSet(),
                State = le == LiftEnum.Holding && (to + 1) == tsof ? LiftEnum.Moving :
                    le == LiftEnum.Moving && (tt + 1) == tpf ? LiftEnum.Holding :
                    dir == 0 ? LiftEnum.Holding : le
            }
        };
    }
    public static int MinOrDefault(this IEnumerable<int> e, int def=int.MaxValue /2) {
        if (e.Any()) return e.Min();
        return def;
    }
    public static int MaxOrDefault(this IEnumerable<int> e, int def=int.MinValue /2) {
        if (e.Any()) return e.Max();
        return def;
    }
}

}
