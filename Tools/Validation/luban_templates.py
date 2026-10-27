"""Generate official Luban fixtures and verify the unified typed-table registry."""
import json
from pathlib import Path
import subprocess
from xml.sax.saxutils import quoteattr


def main():
    root = Path(__file__).resolve().parents[2]
    output = root / 'Artifacts~/Luban/UnifiedTables'
    source = output / 'Source'
    source.mkdir(parents=True, exist_ok=True)
    (source / 'luban.conf').write_text(json.dumps({
        'groups': [{'names': ['c'], 'default': True}],
        'schemaFiles': [{'fileName': 'tables.xml', 'type': ''}],
        'dataDir': '.',
        'targets': [{'name': 'client', 'manager': 'Tables', 'groups': ['c'], 'topModule': 'QgfFixture'}],
    }), encoding='utf-8')
    (source / 'tables.xml').write_text('''<module>
<bean name="Item"><var name="Id" type="int"/><var name="Name" type="string"/><var name="Values" type="list,int"/></bean>
<table name="TbItem" value="Item" input="items.json" index="Id"/>
<table name="TbAlternate" value="Item" input="alternate.json" index="Id"/>
<bean name="Use"><var name="Id" type="int"/><var name="ItemId" type="int#ref=TbItem"/></bean>
<table name="TbUse" value="Use" input="uses.json" index="Id"/>
<bean name="TextRow"><var name="Key" type="string"/><var name="Name" type="string"/></bean>
<table name="TbText" value="TextRow" input="text.json" index="Key"/>
<bean name="ValueRow" valueType="1"><var name="Id" type="int"/><var name="Amount" type="int"/></bean>
<table name="TbValue" value="ValueRow" input="values.json" index="Id"/>
<bean name="Global"><var name="Title" type="string"/><var name="Count" type="int"/><var name="Name" type="string"/></bean>
<table name="TbGlobal" value="Global" input="global.json" mode="one"/>
<bean name="ListRow"><var name="Name" type="string"/></bean>
<table name="TbList" value="ListRow" input="list.json" mode="list"/>
<bean name="Stage"><var name="Level" type="int"/><var name="Wave" type="int"/><var name="Name" type="string"/></bean>
<table name="TbStage" value="Stage" input="stages.json" mode="list" index="Level+Wave"/>
<table name="TbIndexed" value="Stage" input="indexed.json" mode="list" index="Level,Name"/>
<bean name="LongRow"><var name="Key" type="long"/></bean>
<table name="TbLong" value="LongRow" input="long.json" index="Key"/>
<enum name="Kind"><var name="Weapon" value="1"/><var name="Armor" value="2"/></enum>
<bean name="KindRow"><var name="Key" type="Kind"/></bean>
<table name="TbKind" value="KindRow" input="kinds.json" index="Key"/>
<bean name="NumberRow"><var name="Code" type="int"/></bean>
<table name="TbNumber" value="NumberRow" input="numbers.json" index="Code"/>
</module>'''.replace('input="', 'input="*@'), encoding='utf-8')
    for name, rows in {
        'items': [{'Id': 1, 'Name': 'First', 'Values': [3, 5]}, {'Id': 3, 'Name': 'Third', 'Values': []}],
        'alternate': [{'Id': 2, 'Name': 'Second', 'Values': []}],
        'uses': [{'Id': 10, 'ItemId': 1}],
        'text': [{'Key': 'hello', 'Name': 'World'}],
        'values': [{'Id': 20, 'Amount': 42}],
        'global': [{'Title': 'Fixture', 'Count': 5, 'Name': 'Global'}],
        'list': [{'Name': 'List'}],
        'stages': [{'Level': 1, 'Wave': 1, 'Name': 'One'}, {'Level': 1, 'Wave': 2, 'Name': 'Two'}],
        'indexed': [{'Level': 1, 'Wave': 1, 'Name': 'One'}, {'Level': 2, 'Wave': 1, 'Name': 'Two'}],
        'long': [{'Key': 5000000000}],
        'kinds': [{'Key': 'Weapon'}],
        'numbers': [{'Code': 7}],
    }.items():
        (source / (name + '.json')).write_text(json.dumps(rows), encoding='utf-8')
    compiler = root / 'Library/UnityQGameFramework/Tools/Luban/5.1.0/Luban/Luban.dll'
    subprocess.run(['dotnet', str(compiler), '--conf', str(source / 'luban.conf'), '-t', 'client',
                    '--strict', '-c', 'cs-bin', '-d', 'bin', '--customTemplateDir',
                    str(root / 'Packages/com.zheliku.unityqgameframework/Editor/Luban/Templates~'),
                    '-x', 'outputCodeDir=' + str(output / 'Generated'),
                    '-x', 'outputDataDir=' + str(output / 'Data')], check=True)
    # Compile the current engine-independent sources, so this check can run before Unity imports.
    core = root / 'Packages/com.zheliku.unityqgameframework/Runtime/Core'
    luban = root / 'Packages/com.zheliku.unityqgameframework/Runtime/Luban'
    table_loader = root / 'Packages/com.zheliku.unityqgameframework/Runtime/Unity/DataTable/IDataTableLoader.cs'
    sdk_major = int(subprocess.check_output(['dotnet', '--version'], text=True).split('.')[0])
    (output / 'Fixture.csproj').write_text(f'''<Project Sdk="Microsoft.NET.Sdk">
<PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net{sdk_major}.0</TargetFramework><AllowUnsafeBlocks>true</AllowUnsafeBlocks><GenerateAssemblyInfo>false</GenerateAssemblyInfo><EnableDefaultCompileItems>false</EnableDefaultCompileItems><UseAppHost>false</UseAppHost><UseSharedCompilation>false</UseSharedCompilation></PropertyGroup>
<ItemGroup>
<Compile Include={quoteattr(str(core / '**/*.cs'))} Exclude={quoteattr(str(core / 'Config/TomlConfigParser.cs') + ';' + str(core / 'Localization/TomlLocalizationParser.cs'))}/>
<Compile Include={quoteattr(str(luban / '**/*.cs'))}/>
<Compile Include={quoteattr(str(table_loader))}/>
<Compile Include="Generated/**/*.cs"/><Compile Include="Program.cs"/>
</ItemGroup></Project>''', encoding='utf-8')
    (output / 'Program.cs').write_text('''using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using QgfFixture;
using UnityQGameFramework.Core;
using UnityQGameFramework.Core.DataTable;
using UnityQGameFramework.Luban;
using UnityQGameFramework.Runtime;
class Program
{
    static int checks;
    static void Check(bool value, string message) { if (!value) throw new Exception(message); checks++; }
    static void Reject<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { checks++; return; }
        throw new Exception("Expected " + typeof(T).Name);
    }
    static ByteBuf Load(string name) => new ByteBuf(File.ReadAllBytes("Data/" + name + ".bytes"));
    static void Main()
    {
        var tables = new Tables();
        var loaded = ((IDataTableLoader)tables).Load(Load);
        var manager = FrameworkEntry.GetModule<IDataTableManager>();
        manager.RegisterDataTables(loaded);
        Check(manager.Count == 12, "All map/list/one/struct tables must be registered.");
        Check(ReferenceEquals(manager.GetDataTable<TbItem>(), tables.TbItem), "The registry holds the generated table itself.");
        Check(ReferenceEquals(tables.TbItem.GetDataRow(1), tables.TbItem.DataMap[1]), "No copied records.");
        Check(manager.GetDataTable<TbAlternate>().GetDataRow(2).Name == "Second", "Two tables using the same bean.");
        Check(ReferenceEquals(manager.GetDataTable<TbUse>().GetDataRow(10).ItemId_Ref, tables.TbItem.GetDataRow(1)), "Cross-table reference.");
        Check(manager.GetDataTable<TbText>().GetDataRow("hello").Name == "World", "String key.");
        Check(manager.GetDataTable<TbLong>().GetDataRow(5000000000L).Key == 5000000000L, "Int64 key.");
        Check(manager.GetDataTable<TbKind>().HasDataRow(Kind.Weapon), "Enum key.");
        Check(manager.GetDataTable<TbNumber>().GetDataRow(7).Code == 7, "Integer key without an Id field.");
        Check(manager.GetDataTable<TbValue>().GetDataRow(20).Amount == 42, "Value-type record.");
        Check(manager.GetDataTable<TbGlobal>().Data.Title == "Fixture" && tables.TbGlobal.Single().Title == "Fixture", "Singleton table and enumeration.");
        Check(tables.TbGlobal.Count == 1 && tables.TbGlobal.Data.Count == 5 && tables.TbGlobal.Name == "" && tables.TbGlobal.Data.Name == "Global", "Singleton fields do not collide with table metadata.");
        Check(manager.GetDataTable<TbList>().GetDataRow(x => x.Name == "List").Name == "List", "List without a fabricated key.");
        Check(manager.GetDataTable<TbStage>().GetDataRow(1, 2).Name == "Two" && tables.TbStage.HasDataRow(1, 1), "Compound key.");
        Check(manager.GetDataTable<TbIndexed>().GetByLevel(2).Name == "Two" && tables.TbIndexed.GetByName("One").Level == 1, "Independent generated indexes.");
        Check(tables.TbItem.GetDataRow(-1) == null && !tables.TbItem.HasDataRow(-1) && tables.TbText.GetDataRow("missing") == null && tables.TbStage.GetDataRow(99, 1) == null, "Missing keys.");
        Check(tables.TbValue.GetDataRow(-1).Equals(default(ValueRow)), "Missing value-type record.");
        Reject<KeyNotFoundException>(() => tables.TbItem.Get(-1));
        var results = new List<Item> { tables.TbItem.GetDataRow(1) };
        tables.TbItem.GetDataRows(row => row.Id >= 1, (a,b) => b.Id.CompareTo(a.Id), results);
        Check(results.Count == 2 && results[0].Id == 3 && tables.TbItem.DataList[0].Id == 1, "Filtering/sorting preserves table order.");
        Check(tables.TbItem.HasDataRow(row => row.Values.Count == 2), "Containers and predicate query.");
        var named = new TbItem(Load("tbitem"), "Named");
        manager.RegisterDataTables(new[] { named });
        Check(ReferenceEquals(manager.GetDataTable<TbItem>("Named"), named), "Named generated instance.");
        var pending = new TbItem(Load("tbitem"), "Pending");
        Reject<FrameworkException>(() => manager.RegisterDataTables(new DataTableBase[] { pending, named }));
        Check(!manager.HasDataTable<TbItem>("Pending"), "Registration is atomic.");
        Check(!manager.DestroyDataTable(new TbItem(Load("tbitem"), "Named")), "A different instance cannot remove a registered table.");
        Check(manager.DestroyDataTable(named) && !manager.HasDataTable<TbItem>("Named"), "Instance removal.");
        Check(manager.GetAllDataTables().All(table => table.Count > 0 && table.Type != table.RowType), "Complete metadata and row enumeration.");
        Reject<ArgumentException>(() => manager.GetDataTable(typeof(Item)));
        FrameworkEntry.Shutdown();
        Check(manager.Count == 0 && tables.TbUse.Get(10).ItemId_Ref.Name == "First", "Unload drops registry ownership without corrupting retained records.");
        File.WriteAllText("result.json", "{\\"passed\\":true,\\"tables\\":12,\\"checks\\":" + checks + "}");
        Console.WriteLine("Passed: " + checks + " unified table checks.");
    }
}''', encoding='utf-8')
    subprocess.run(['dotnet', 'run', '--project', str(output / 'Fixture.csproj'), '--configuration', 'Release'], cwd=output, check=True)


if __name__ == '__main__':
    main()
