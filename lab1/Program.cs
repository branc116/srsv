using System.IO.Pipes;

var name = args.Aggregate((i, j) => $"{i} {j}");
var time_start = DateTime.Now;

var task = args[0] switch {
    "semafor" => handleSemaforAsync(int.Parse(args[1])),
    "UPR" => handleUPR(),
    "Auto" => handleAutoAsync(int.Parse(args[1]), int.Parse(args[2])),
    "Biciklo" => handleBicikloAsync(int.Parse(args[1]), int.Parse(args[2])),
    "output" => handleOutput(args[1]),
    _ => throw new NotImplementedException($"args: {args[0]}")
};
await task;



void Log(object o) {
    System.Console.WriteLine($"[${DateTime.Now - time_start}][{name}] {o}");
}

async Task<string> readLine<T>(T stream) where T: Stream {
    var str = "";
    var b = new byte[1];
    r: var n = await stream.ReadAsync(b, 0, 1);
    if (n < 1)
        return str;
    if (((char)b[0]) == '\n')
        return str;
    str += (char)b[0];
    goto r;
}

string GetSemeaforDir(int id) => id switch {
    0 => "Istok/Zapad",
    1 => "Sjever/Jug",
    _ => throw new KeyNotFoundException($"Semafor name by id. ID: {id}")
};
async Task handleSemaforAsync(int id) {
    var numOfClients = 0;
    var curColor = "crveno";
    var toNotify = new Queue<string>();
    while(true) {
        numOfClients = 0;
        var a = await $"semafor{id}".ReadLineAsync();
        Log($"poruka {a}");
        var split =  a.Split(' ');
        var t = split[0] switch {
            "zeleno" => ZelenoToAll(),
            "crveno" => Crveno(),
            "notif" => NotifMeWhenZeleno(split[1]),
            "get" => SendMeYourColor(split[1]),
            "" => Nope(),
            _ => throw new NotImplementedException($"Unknown message {a}")
        };
        await t;
    }
    async Task Nope() => await Task.Delay(1000);
    async Task Crveno() {
        curColor = "crveno";
    }
    async Task SendMeYourColor(string pipeName) {
        await pipeName.WriteAsync(curColor);
    }
    async Task NotifMeWhenZeleno(string pipeName) {
        toNotify.Enqueue(pipeName);
    }
    async Task ZelenoToAll() {
        curColor = "zeleno";
        while(toNotify.Any()) {
            var pipe = toNotify.Dequeue();
            Log($"Notifing: {pipe}");
            await pipe.WriteAsync(curColor);
        }
    }
}

async Task handleUPR()
{
    int id = 0;
    int tally = 0;
    var tasks = new Func<Task>[] { async () => 
        {
            while(true) {
                Log($"Smjer {GetSemeaforDir(id)}");
                await $"semafor{id}".WriteAsync("zeleno");
                await Task.Delay(3000);
                await $"semafor{id}".WriteAsync($"crveno");
                await Task.Delay(150);
                tally += ((id * 2) - 1) * 3;
                id = (id + 1) % 2;
                //id = tally > 0 ? 0 : 1;
                //tally = 0;
            }
        },
        async () => {
            while(true) {
                var line = await "upr".ReadLineAsync();
                Log($"Poruka: {line}");
                var split = line.Split(' ');
                var t = split[0] switch {
                    "bogB" => noviBiciklo(int.Parse(split[1])),
                    "bogA" => noviAuto(int.Parse(split[1])),
                    "caoB" => noviBiciklo(int.Parse(split[1]), false),
                    "caoA" => noviAuto(int.Parse(split[1]), false),
                    "ProlazimB" => prolaziBiciklo(),
                    "ProlazimA" => prolaziAuto(),
                    string s => throw new NotImplementedException(s)
                };
                await t;
            }
        }
    }.Select(i => i());
    await Task.WhenAll(tasks);
    Task noviBiciklo(int dir, bool dolazak = true) {
        Log($"Bicilko {(dolazak ? "Došlo" : "Ošlo")}");
        tally += ((dir * 2) - 1) * 2;
        return Task.CompletedTask;
    }
    Task noviAuto(int dir, bool dolazak = true) {
        Log($"Auto {(dolazak ? "Došo" : "Ošo")}");
        tally += ((dir * 2) - 1);
        return Task.CompletedTask;
    }
    Task prolaziBiciklo() {
        Log($"Bicilko prolazi");
        return Task.CompletedTask;
    }
    Task prolaziAuto() {
        Log($"Auto Prolazi");
        return Task.CompletedTask;
    }
}
async Task handleBicikloAsync(int v, int id)
{
    await $"upr".WriteAsync($"bogB {v}");
    var myPipe = $"Biciklo{id}";
    await $"semafor{v}".WriteAsync($"get {myPipe}");
    var color = await myPipe.ReadLineAsync();
    Log($"Boja: {color}");
    if (color == "crveno") {
        await $"semafor{v}".WriteAsync($"notif {myPipe}");
        Log($"Zatražio obavjest za zelenim");
        var text = await myPipe.ReadLineAsync();
        Log($"Dobio obavjest da je zeleno");
    }
    await $"upr".WriteAsync($"ProlazimB {v}");
    Log($"Prolazim");
    await Task.Delay(1000);
    await $"upr".WriteAsync($"caoB {v}");
    Log($"Prošao");
}
(int[] b, int[] a, int[] bp, int[] ap) parse(string[] lines) {
    var b = new int[2];
    var bp = new int[2];
    var a = new int[2];
    var ap = new int[2];
    foreach(var i in lines) {
        if (i.Contains("bogB")) {
            var dir = int.Parse(i.Split(' ').Last());
            b[dir]++;
        }
        else if (i.Contains("bogA")) {
            var dir = int.Parse(i.Split(' ').Last());
            a[dir]++;
        }
        else if (i.Contains("ProlazimB")) {
            var dir = int.Parse(i.Split(' ').Last());
            b[dir]--;
            bp[dir]++;
        }
        else if (i.Contains("ProlazimA")) {
            var dir = int.Parse(i.Split(' ').Last());
            ap[dir]++;
            a[dir]--;
        }
        else if (i.Contains("caoB")) {
            var dir = int.Parse(i.Split(' ').Last());
            bp[dir]--;
        }
        else if (i.Contains("caoA")) {
            var dir = int.Parse(i.Split(' ').Last());
            ap[dir]--;
        }
    }
    return (b, a, bp, ap);
}
async Task handleOutput(string file) {
    var A = Enumerable.Range(0, 5).Select(i => (int)Math.Pow(10, i));
    m: var lines = await System.IO.File.ReadAllLinesAsync(file);
    var (b, a, bp, e) = parse(lines);
    
    A.ToList().ForEach(Console.WriteLine);
    var B = A.Select(i => bp[1]/i).ToArray(); //koji prolaze
    var d = A.Select(i => a[1]/i).ToArray();
    var D = A.Select(i => e[1]/i).ToArray();
    var message = @$"
        |     |     |          
        |     |     |          
        /{bp[0]:D11}|          
        ------------|{b[0]:D8}  
        \ | | | | | |          
---------      /|\  -/|\-------
                |{d[3]}   -|{B[4]}        
              | |{d[2]}   -|{B[3]}        
-----    ---- | |{d[1]}   -|{B[2]}   ---- 
{a[0]:D9}\    | |{d[0]}   -|{B[1]}        
---------{e[0]:D9}   -|{B[0]}        
--------/           -----------
        |      /|\  |{b[1]:D8}
        |     | |{d[3]}   |          
        |     | |{d[2]}   |          
        |     | |{d[1]}   |          
        |     | |{d[0]}   |          ";
        System.Console.WriteLine(message); await Task.Delay(100); goto m;
}

async Task handleAutoAsync(int v, int id)
{
    await $"upr".WriteAsync($"bogA {v}");
    var myPipe = $"Auto{id}";
    await $"semafor{v}".WriteAsync($"get {myPipe}");
    var color = await myPipe.ReadLineAsync();
    Log($"Boja: {color}");
    if (color == "crveno") {
        await $"semafor{v}".WriteAsync($"notif {myPipe}");
        Log($"Zatražio obavjest za zelenim");
        var text = await myPipe.ReadLineAsync();
        Log($"Dobio obavjest da je zeleno");
    }
    await $"upr".WriteAsync($"ProlazimA {v}");
    Log($"Prolazim");
    await Task.Delay(1000);
    await $"upr".WriteAsync($"caoA {v}");
    Log($"Prošao");
}

public static class Helper {
    public static async Task WriteAsync(this Stream s, string str) {
        await s.WriteAsync(System.Text.Encoding.ASCII.GetBytes($"{str}\n").AsMemory());
    }
    public static async Task WriteAsync(this string pipeName, string str) {
        using var pipe = new NamedPipeClientStream(pipeName);
        await pipe.ConnectAsync();
        await pipe.WriteAsync(System.Text.Encoding.ASCII.GetBytes($"{str}\n").AsMemory());
        await pipe.DisposeAsync();
    }
    public static async Task<string> ReadLineAsync(this string pipeName) {
        using var pipe = new NamedPipeServerStream(pipeName);
        await pipe.WaitForConnectionAsync();
        var str = "";
        var b = new byte[1];
        r: var n = await pipe.ReadAsync(b, 0, 1);
        if (n < 1)
            goto ret;
        if (((char)b[0]) == '\n')
            goto ret;
        str += (char)b[0];
        goto r;
        ret: pipe.Disconnect();
        await pipe.DisposeAsync();
        return str;
    }
}