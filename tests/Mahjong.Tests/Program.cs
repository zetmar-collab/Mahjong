using System.Diagnostics;
using System.Text.Json;
using Mahjong.Core;

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
Console.WriteLine($"RESULT: {passed} suites passed, {failed} failed, {solutions} full solutions replayed, {watch.Elapsed.TotalSeconds:F2}s");
Environment.ExitCode=failed==0?0:1;
