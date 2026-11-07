using System.Diagnostics;
using System.Text.Json;
using Mahjong.Core;

if (args.Length == 2 && args[0] == "--lease-probe")
{
    using var lease = InstanceLease.TryAcquire(args[1]); Environment.ExitCode = lease == null ? 42 : 0; return;
}

int passed=0, failed=0, solutions=0;
var watch=Stopwatch.StartNew();
string assets=Path.Combine(AppContext.BaseDirectory,"Assets");
List<Layout> layouts=[];
void Check(bool condition,string message="Assertion failed") { if(!condition) throw new Exception(message); }
void Test(string name,Action run)
{
    try {run();passed++;Console.WriteLine("PASS "+name);}
    catch(Exception ex) {failed++;Console.WriteLine("FAIL "+name+": "+ex.Message);}
}
Symbol S(string id="a")=>new(id,id,"geometry",1);
GameState G(List<Position> p,params (int,Symbol)[] t)=>new(){Positions=p,Tiles=t.ToDictionary(x=>x.Item1,x=>x.Item2)};
string Signature(IEnumerable<Symbol> symbols)=>string.Join("|",symbols.GroupBy(s=>s.Id).OrderBy(g=>g.Key).Select(g=>g.Key+":"+g.Count()));
GameState Clone(GameState g)=>JsonSerializer.Deserialize<GameState>(JsonSerializer.Serialize(g,Storage.Json),Storage.Json)!;

Test("Layouts: size, uniqueness, same-layer intersections and complete support",()=>{layouts=Catalog.LoadLayouts(Path.Combine(assets,"layouts.json"));Check(layouts.Count==6);Check(layouts.Select(l=>l.Count).SequenceEqual(new[]{36,72,108,144,144,144}));});
Test("Covered tile, partial overlap and multiple covering tiles",()=>{
 var p=new Position(0,0,0,0);var q=new Position(1,1,1,1);Check(!Rules.Free(p,[p,q]));Check(Rules.Free(p,[p,new(1,2,0,1)]));
 Check(!Rules.Free(p,[p,new(1,-1,0,1),new(2,1,0,1)]));Check(!Rules.Free(p,[p,new(1,.5,1.5,2)]));
});
Test("Both sides blocked; left/right free; vertical edges do not unlock",()=>{
 var p=new Position(0,2,2,0);var left=new Position(1,0,2,0);var right=new Position(2,4,2,0);
 Check(!Rules.Free(p,[p,left,right]));Check(Rules.Free(p,[p,right]));Check(Rules.Free(p,[p,left]));Check(Rules.Free(p,[p,new(3,2,0,0),new(4,2,4,0)]));
 Check(!Rules.Free(p,[p,new(1,0,1,0),new(2,4,3,0)]));
});
Test("Ordinary matches, flowers, seasons and distinct identity",()=>{
 Check(Rules.Matches(S(),S()));Check(!Rules.Matches(S(),S("b")));
 var deck=Catalog.Deck("classic",144);var flowers=deck.Where(s=>s.Family=="flower").ToArray();var seasons=deck.Where(s=>s.Family=="season").ToArray();
 Check(Rules.Matches(flowers[0],flowers[3]));Check(Rules.Matches(seasons[0],seasons[3]));Check(!Rules.Matches(flowers[0],seasons[0]));
 var game=G([new(0,0,0,0),new(1,2,0,0)],(0,S()),(1,S()));Check(!Rules.Remove(game,0,0));Check(game.Tiles.Count==2);Check(Rules.Remove(game,0,1));Check(!Rules.Remove(game,0,1));
});
Test("Blocked or mismatched pair never removed",()=>{
 var game=G([new(0,0,0,0),new(1,2,0,0),new(2,0,0,1),new(3,4,0,0)],(0,S()),(1,S()),(2,S("b")),(3,S()));
 Check(!Rules.Remove(game,0,3));Check(!Rules.Remove(game,2,3));Check(game.Tiles.Count==4);
});
Test("All decks have even matching classes and classic exact composition",()=>{
 foreach(var set in Catalog.Sets)foreach(var count in new[]{36,72,108,144}){var deck=Catalog.Deck(set,count);Check(deck.Count==count);Check(deck.GroupBy(s=>s.Match).All(g=>g.Count()%2==0));}
 var d=Catalog.Deck("classic",144);Check(d.Count(s=>s.Family is "circles" or "bamboo" or "characters")==108);Check(d.Count(s=>s.Family=="wind")==16);Check(d.Count(s=>s.Family=="dragon")==12);Check(d.Count(s=>s.Family=="flower")==4);Check(d.Count(s=>s.Family=="season")==4);
});
Test("Invalid layout rejected",()=>{
 foreach(var l in new[]{new Layout("bad",2,[new(0,0,0,0),new(0,2,0,0)]),new Layout("bad",2,[new(0,0,0,0),new(1,1,0,0)]),new Layout("bad",2,[new(0,0,0,0),new(1,8,0,1)])}){
 bool rejected=false;try{Rules.Validate(l);}catch(InvalidDataException){rejected=true;}Check(rejected);}
});
Test("360 complete generated solutions: six layouts x three decks x 20 fixed seeds",()=>{
 foreach(var l in layouts)foreach(var set in Catalog.Sets)foreach(int seed in Enumerable.Range(0,20)){
 var game=Generator.New(l,set,seed*7919);Check(Generator.Replay(game,game.Solution),$"{l.Id}/{set}/{seed}");solutions++;
 var again=Generator.New(l,set,seed*7919);Check(JsonSerializer.Serialize(game.Tiles)==JsonSerializer.Serialize(again.Tiles),"Seed reproducibility");
 }
});
Test("Hints always refer to two free, distinct matching positions",()=>{
 foreach(var l in layouts)foreach(var set in Catalog.Sets){var game=Generator.New(l,set,77);foreach(var step in game.Solution.ToArray()){
 var hints=Rules.Pairs(game);Check(hints.Count>0);foreach(var hint in hints){var clone=Clone(game);Check(hint.A!=hint.B && Rules.Remove(clone,hint.A,hint.B));}Check(Rules.Remove(game,step.A,step.B));}Check(Rules.Pairs(game).Count==0);}
});
Test("No moves is distinct from victory",()=>{var game=G([new(0,0,0,0),new(1,2,0,0)],(0,S()),(1,S("b")));Check(game.Tiles.Count>0 && Rules.Pairs(game).Count==0);});
Test("Shuffle: exact symbols, positions, removed positions, counters and full solution",()=>{
 foreach(var l in layouts)foreach(var set in Catalog.Sets){var game=Generator.New(l,set,199);foreach(var pair in game.Solution.Take(5).ToList())Check(Rules.Remove(game,pair.A,pair.B));
 game.Seconds=81;game.Hints=3;var before=Signature(game.Tiles.Values);var ids=game.Tiles.Keys.Order().ToArray();var positions=game.Positions.ToArray();
 Check(Generator.Shuffle(game,390));Check(Signature(game.Tiles.Values)==before);Check(game.Tiles.Keys.Order().SequenceEqual(ids));Check(game.Positions.SequenceEqual(positions));Check(game.Seconds==81 && game.Hints==3 && game.ShuffleUsed);Check(Generator.Replay(game,game.Solution));solutions++;Check(!Generator.Shuffle(game,12));
 }
});
Test("Failed shuffle preserves every byte of state and allowance",()=>{
 var game=Generator.New(layouts[0],"geometry",4);string before=JsonSerializer.Serialize(game);Check(!Generator.Shuffle(game,9,0));Check(JsonSerializer.Serialize(game)==before);
 // This remaining geometry cannot be cleared: the single cap blocks the only two bottom tiles.
 game=G([new(0,0,0,0),new(1,2,0,0),new(2,1,0,1),new(3,0,0,2)],(0,S()),(1,S()),(2,S()),(3,S()));before=JsonSerializer.Serialize(game);Check(!Generator.Shuffle(game,9));Check(JsonSerializer.Serialize(game)==before);
});
Test("Atomic save round trip and second shuffle blocked after reload",()=>{
 string folder=Path.Combine(Path.GetTempPath(),"MahjongTests-"+Guid.NewGuid());string path=Path.Combine(folder,"game.json");
 try{var game=Generator.New(layouts[3],"classic",41);var move=game.Solution[0];Rules.Remove(game,move.A,move.B);Check(Generator.Shuffle(game,111));game.Seconds=70.25;game.Hints=2;Storage.Atomic(path,game);
 Check(Storage.Load(path,layouts,out var loaded));Check(JsonSerializer.Serialize(game,Storage.Json)==JsonSerializer.Serialize(loaded,Storage.Json));Check(!Generator.Shuffle(loaded!,112));Check(!File.Exists(path+".tmp"));
 }finally{if(Directory.Exists(folder))Directory.Delete(folder,true);}
});
Test("Corrupt, unknown version, invalid symbols and invalid counters rejected",()=>{
 string path=Path.Combine(Path.GetTempPath(),"MahjongInvalid-"+Guid.NewGuid()+".json");
 try{foreach(var json in new[]{"{bad","null","{}","{\"Positions\":null}","[]"}){File.WriteAllText(path,json);Check(!Storage.Load(path,layouts,out _));}
 var original=Generator.New(layouts[0],"geometry",2);foreach(var variant in Enumerable.Range(0,7)){var g=Clone(original);switch(variant){case 0:g.Version=99;break;case 1:g.Seconds=-1;break;case 2:g.Tiles[0]=S("fake");break;case 3:g.Hints=-3;break;case 4:g.Initial.Clear();break;case 5:g.Positions[0]=new(0,999,0,0);break;case 6:g.Tiles.Remove(0);break;}Storage.Atomic(path,g);Check(!Storage.Load(path,layouts,out _),"Invalid save variant "+variant);}
 }finally{File.Delete(path);}
});
Test("PL/EN key completeness and Polish number forms",()=>{
 var t=new Localizer(assets,"pl");Check(t.Polish.Count>110);Check(t.Polish.Keys.Order().SequenceEqual(t.English.Keys.Order()));
 foreach(var kind in new[]{"tile","pair"})foreach(var (n,suffix) in new[]{(1,"One"),(2,"Few"),(5,"Many"),(12,"Many"),(13,"Many"),(14,"Many"),(22,"Few"),(23,"Few"),(24,"Few"),(112,"Many"),(122,"Few")})Check(t.Number(n,kind)==n+" "+t[kind+suffix]);
 t.Language="en";Check(t.Number(1,"tile")=="1 tile");Check(t.Number(22,"pair")=="22 pairs");
});
Test("Seed and solution are persisted explicitly, not used as replacement for board state",()=>{var game=Generator.New(layouts[1],"nature",1234567);var json=JsonSerializer.Serialize(game);var loaded=JsonSerializer.Deserialize<GameState>(json)!;Check(loaded.Seed==1234567);Check(Generator.Replay(loaded,loaded.Solution));Check(loaded.Initial.Count==72);});
Test("Finished results persist all metadata once per game",()=>{
 string dir=Path.Combine(Path.GetTempPath(),"MahjongResults-"+Guid.NewGuid());string file=Path.Combine(dir,"results.json");
 try { var game=Generator.New(layouts[0],"classic",721);foreach(var pair in game.Solution)Check(Rules.Remove(game,pair.A,pair.B));
 game.Seconds=123.25;game.Hints=7;game.ShuffleUsed=true;var date=new DateTimeOffset(2026,10,28,21,30,0,TimeSpan.Zero);
 var result=ResultStore.SaveFinished(file,game,"  Łukasz  ",date);var all=ResultStore.Load(file);
 Check(all.Count==1 && all[0]==result);Check(result.Player=="Łukasz" && result.Seconds==123.25 && result.Hints==7 && result.ShuffleUsed && result.Seed==721 && result.FinishedAt==date);
 var second=ResultStore.SaveFinished(file,game,"Duplicate");Check(second==result && ResultStore.Load(file).Count==1);
 } finally {if(Directory.Exists(dir))Directory.Delete(dir,true);}
});
Test("Results reject incomplete games and invalid player names",()=>{
 string file=Path.Combine(Path.GetTempPath(),"MahjongPending-"+Guid.NewGuid()+".json");var game=Generator.New(layouts[0],"geometry",1);
 bool refused=false;try{ResultStore.SaveFinished(file,game,"Player");}catch(InvalidOperationException){refused=true;}Check(refused && !File.Exists(file));
 foreach(var pair in game.Solution)Rules.Remove(game,pair.A,pair.B);
 foreach(var name in new[]{new string('x',33),"Player\nName"}){refused=false;try{ResultStore.SaveFinished(file,game,name);}catch(InvalidOperationException){refused=true;}Check(refused && !File.Exists(file));}
});
Test("Corrupt result history is preserved when saving fails",()=>{
 string file=Path.Combine(Path.GetTempPath(),"MahjongCorruptResults-"+Guid.NewGuid()+".json");
 try {const string data="[{ broken history";File.WriteAllText(file,data);var game=Generator.New(layouts[0],"nature",1);foreach(var pair in game.Solution)Rules.Remove(game,pair.A,pair.B);
 bool refused=false;try{ResultStore.SaveFinished(file,game,"");}catch(JsonException){refused=true;}Check(refused && File.ReadAllText(file)==data);
 }finally{File.Delete(file);}
});
Test("Result history rejects invalid entries and sorts newest first",()=>{
 string file=Path.Combine(Path.GetTempPath(),"MahjongHistory-"+Guid.NewGuid()+".json");
 var a=new GameResult(Guid.NewGuid(),"A",DateTimeOffset.UtcNow.AddDays(-1),"glade","geometry",36,10,0,false,1);var b=a with {GameId=Guid.NewGuid(),FinishedAt=DateTimeOffset.UtcNow};
 try {Storage.Atomic(file,new[]{a,b});Check(ResultStore.Load(file)[0]==b);
 foreach(var entries in new[]{new[]{a,a},new[]{a with {Seconds=-1}},new[]{a with {Seconds=1e100}},new[]{a with {TileCount=144}},new[]{a with {LayoutId="unknown"}},new[]{a with {Player="x\ny"}},new[]{a with {TileCount=1}}}){
 Storage.Atomic(file,entries);bool refused=false;try{ResultStore.Load(file);}catch(InvalidDataException){refused=true;}Check(refused);}
 }finally{File.Delete(file);}
});
Test("Bounded data reads, overflow time and total-hour formatting",()=>{
 string dir=Path.Combine(Path.GetTempPath(),"MahjongBounds-"+Guid.NewGuid());Directory.CreateDirectory(dir);string path=Path.Combine(dir,"game.json");
 try {var game=Generator.New(layouts[0],"geometry",9);game.Seconds=1e100;Storage.Atomic(path,game);Check(!Storage.Load(path,layouts,out _));
 game.Seconds=GameTime.MaxSeconds;Storage.Atomic(path,game);Check(Storage.Load(path,layouts,out _));Check(GameTime.Format(90061)=="25:01:01");Check(GameTime.Format(1e100)=="--:--:--");
 File.WriteAllText(path,new string('x',100));bool refused=false;try{Storage.ReadBounded(path,99);}catch(InvalidDataException){refused=true;}Check(refused);
 }finally{Directory.Delete(dir,true);}
});
Test("Positive solver budget is honored and negative budget is rejected transactionally",()=>{
 var game=Generator.New(layouts[3],"classic",61);string before=JsonSerializer.Serialize(game);
 Check(!Generator.Shuffle(game,62,1));Check(JsonSerializer.Serialize(game)==before);
 bool rejected=false;try{Generator.Shuffle(game,62,-1);}catch(ArgumentOutOfRangeException){rejected=true;}
 Check(rejected && JsonSerializer.Serialize(game)==before);
});
Test("Single data owner across processes and recovery after release",()=>{
 string dir=Path.Combine(Path.GetTempPath(),"MahjongLease-"+Guid.NewGuid());
 int Child(){var start=new ProcessStartInfo(Environment.ProcessPath!){UseShellExecute=false,CreateNoWindow=true};start.ArgumentList.Add("--lease-probe");start.ArgumentList.Add(dir);using var process=Process.Start(start)!;Check(process.WaitForExit(10000));return process.ExitCode;}
 try {using(var lease=InstanceLease.TryAcquire(dir)){Check(lease!=null);Check(Child()==42);}Check(Child()==0);}
 finally{Directory.Delete(dir,true);}
});
Test("Undo persists, preserves counters and cannot cross shuffle or victory",()=>{
 var game=Generator.New(layouts[0],"classic",64);var original=JsonSerializer.Serialize(game.Tiles);var pair=game.Solution[0];
 Check(GameActions.Remove(game,pair.A,pair.B));game.Seconds=90061;game.Hints=3;Check(GameActions.ValidHistory(game));
 var loaded=Clone(game);Check(Storage.Validate(loaded,layouts,out _));Check(GameActions.Undo(loaded));
 Check(loaded.Tiles.Count==36 && loaded.Tiles.All(t=>game.Initial[t.Key]==t.Value));Check(loaded.UndoCount==1 && loaded.Hints==3 && loaded.Seconds==90061 && !loaded.ShuffleUsed);
 Check(!GameActions.Undo(loaded));Check(Generator.Shuffle(game,65));Check(game.Moves.Count==0 && !GameActions.Undo(game) && game.ShuffleUsed);
 var next=game.Solution[0];Check(GameActions.Remove(game,next.A,next.B));Check(GameActions.Undo(game) && game.ShuffleUsed);
 foreach(var p in game.Solution)Check(GameActions.Remove(game,p.A,p.B));Check(game.CompletedAt!=null && !GameActions.Undo(game));
 var corrupt=Clone(loaded);corrupt.Moves.Add(new(0,corrupt.Initial[0],1,corrupt.Initial[1]));Check(!Storage.Validate(corrupt,layouts,out _));
});
Test("Automatic result can be named once, retains finish date and assistance",()=>{
 string dir=Path.Combine(Path.GetTempPath(),"MahjongAuto-"+Guid.NewGuid());string path=Path.Combine(dir,"results.json");
 try{var game=Generator.New(layouts[0],"geometry",11);foreach(var p in game.Solution)Check(GameActions.Remove(game,p.A,p.B));
 game.CompletedAt=DateTimeOffset.UtcNow.AddDays(-1);game.UndoCount=2;
 var auto=ResultStore.SaveFinished(path,game,"",automatic:true);Check(!auto.NameConfirmed && auto.FinishedAt==game.CompletedAt && auto.UndoCount==2);
 var named=ResultStore.SaveFinished(path,game,"Hanna");Check(named.NameConfirmed && named.Player=="Hanna" && named.FinishedAt==auto.FinishedAt);Check(ResultStore.Load(path).Count==1);
 Check(ResultStore.SaveFinished(path,game,"Other")==named);Check(ResultStore.SaveFinished(path,game,"",automatic:true)==named);
 }finally{Directory.Delete(dir,true);}
});
Test("Old version-one saves and legacy result entries remain compatible",()=>{
 var game=Generator.New(layouts[0],"nature",15);var node=System.Text.Json.Nodes.JsonNode.Parse(JsonSerializer.Serialize(game))!;
 foreach(var key in new[]{"CompletedAt","UndoCount","Moves","ChallengeDate"})node.AsObject().Remove(key);
 Check(Storage.Validate(JsonSerializer.Deserialize<GameState>(node.ToJsonString()),layouts,out var loaded));Check(loaded!.UndoCount==0 && loaded.Moves.Count==0);
 var json="{\"GameId\":\""+Guid.NewGuid()+"\",\"Player\":\"Old\",\"FinishedAt\":\"2026-10-29T00:00:00Z\",\"LayoutId\":\"glade\",\"SetId\":\"geometry\",\"TileCount\":36,\"Seconds\":12,\"Hints\":0,\"ShuffleUsed\":false,\"Seed\":1}";
 var result=JsonSerializer.Deserialize<GameResult>(json)!;Check(result.NameConfirmed && result.UndoCount==0);
});
Test("Deal codes preserve every original symbol and reject altered payloads",()=>{
 foreach(var l in layouts)foreach(var set in Catalog.Sets){var game=Generator.New(l,set,216);var code=DealCode.Encode(game);var decoded=DealCode.Decode(code,layouts);
 Check(code.Length<1024 && decoded.GameId!=game.GameId && decoded.Initial.OrderBy(t=>t.Key).SequenceEqual(game.Initial.OrderBy(t=>t.Key)));Check(Generator.Replay(decoded,decoded.Solution));
 var shuffled=Clone(game);Check(Generator.Shuffle(shuffled,217));Check(DealCode.Encode(shuffled)==code);
 bool refused=false;try{DealCode.Decode(code[..8]+(code[8]=='A'?'B':'A')+code[9..],layouts);}catch(InvalidDataException){refused=true;}Check(refused);}
 foreach(string bad in new[]{"","MJ2-abcd","MJ1-!", "MJ1-"+new string('a',1025)}){bool refused=false;try{DealCode.Decode(bad,layouts);}catch(InvalidDataException){refused=true;}Check(refused);}
});
Test("Daily challenge is date-specific, deterministic and fully solvable",()=>{
 string? previous=null;foreach(int offset in Enumerable.Range(0,31)){var day=new DateOnly(2026,11,1).AddDays(offset);var a=DealCode.Daily(day,layouts);var b=DealCode.Daily(day,layouts);
 var code=DealCode.Encode(a);Check(code==DealCode.Encode(b) && code!=previous);Check(a.ChallengeDate==day.ToString("yyyy-MM-dd") && Generator.Replay(a,a.Solution));previous=code;}
});
Test("History filters keep assisted, unassisted and challenge records separate",()=>{
 var a=new GameResult(Guid.NewGuid(),"A",DateTimeOffset.UtcNow,"glade","geometry",36,20,0,false,1);
 var b=a with{GameId=Guid.NewGuid(),Seconds=10,UndoCount=1};var c=a with{GameId=Guid.NewGuid(),LayoutId="turtle",TileCount=144,Seconds=5,ChallengeDate="2026-11-07"};
 var rows=new[]{a,b,c};Check(ResultQuery.Filter(rows,"glade","geometry","unassisted").Single()==a);Check(ResultQuery.Filter(rows,help:"assisted").Single()==b);
 Check(ResultQuery.Filter(rows,order:"fastest").First()==c);Check(ResultQuery.Filter(rows,daily:true).Single()==c);
});
Test("Backup round-trip preserves data and retains pre-import files",()=>{
 string dir=Path.Combine(Path.GetTempPath(),"MahjongBackup-"+Guid.NewGuid());string source=Path.Combine(dir,"source"),target=Path.Combine(dir,"target"),archive=Path.Combine(dir,"copy.mahjongbackup");Directory.CreateDirectory(source);Directory.CreateDirectory(target);
 try{var game=Generator.New(layouts[0],"classic",31);var p=game.Solution[0];GameActions.Remove(game,p.A,p.B);Storage.Atomic(Path.Combine(source,"game.json"),game);Storage.Atomic(Path.Combine(source,"settings.json"),new Preferences());
 File.WriteAllText(Path.Combine(target,"game.json"),"previous data");BackupService.Export(source,archive);string previous=BackupService.Import(archive,target,layouts,_=>throw new Exception("No image expected"));
 Check(File.ReadAllText(Path.Combine(previous,"game.json"))=="previous data");Check(Storage.Load(Path.Combine(target,"game.json"),layouts,out var loaded) && loaded!.GameId==game.GameId && loaded.Moves.Count==1);
 }finally{Directory.Delete(dir,true);}
});
Test("Backup rejects path traversal, checksum damage and invalid saves before replacement",()=>{
 string dir=Path.Combine(Path.GetTempPath(),"MahjongBadBackup-"+Guid.NewGuid());Directory.CreateDirectory(dir);string target=Path.Combine(dir,"data");Directory.CreateDirectory(target);string sentinel=Path.Combine(target,"settings.json");File.WriteAllText(sentinel,"unchanged");
 try{foreach(string entryName in new[]{"../outside.json","game.json"}){string zipPath=Path.Combine(dir,Guid.NewGuid()+".zip");using(var zip=System.IO.Compression.ZipFile.Open(zipPath,System.IO.Compression.ZipArchiveMode.Create)){
 using(var writer=new StreamWriter(zip.CreateEntry("manifest.json").Open()))writer.Write(JsonSerializer.Serialize(new{Version=1,Hashes=new Dictionary<string,string>{{entryName,"BAD"}}}));
 using(var writer=new StreamWriter(zip.CreateEntry(entryName).Open()))writer.Write("bad");}
 bool refused=false;try{BackupService.Import(zipPath,target,layouts,_=>{});}catch(InvalidDataException){refused=true;}Check(refused && File.ReadAllText(sentinel)=="unchanged");Check(!File.Exists(Path.Combine(target,"outside.json")));}
 }finally{Directory.Delete(dir,true);}
});

Console.WriteLine($"RESULT: {passed} suites passed, {failed} failed, {solutions} full solutions replayed, {watch.Elapsed.TotalSeconds:F2}s");
Environment.ExitCode=failed==0?0:1;
