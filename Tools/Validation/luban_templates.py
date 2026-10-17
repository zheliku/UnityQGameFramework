"""Compile real Luban fixtures and exercise native/QGF queries outside Unity.

Run after the host Unity project has compiled the current package. Outputs stay
under Artifacts~ and never add generated fixture types to the host's Assets.
"""
import json
from pathlib import Path
import subprocess
from xml.sax.saxutils import quoteattr


def main():
    root = Path(__file__).resolve().parents[2]
    output = root / 'Artifacts~/Luban/AdvancedTemplates'
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
<bean name="Global"><var name="Title" type="string"/></bean>
<table name="TbGlobal" value="Global" input="global.json" mode="one"/>
<bean name="ListRow"><var name="Name" type="string"/></bean>
<table name="TbList" value="ListRow" input="list.json" mode="list"/>
</module>'''.replace('input="', 'input="*@'), encoding='utf-8')
    for name, rows in {
        'items': [{'Id': 1, 'Name': 'First', 'Values': [3, 5]}],
        'alternate': [{'Id': 2, 'Name': 'Second', 'Values': []}],
        'uses': [{'Id': 10, 'ItemId': 1}],
        'text': [{'Key': 'hello', 'Name': 'World'}],
        'values': [{'Id': 20, 'Amount': 42}],
        'global': [{'Title': 'Fixture'}],
        'list': [{'Name': 'List'}],
    }.items():
        (source / (name + '.json')).write_text(json.dumps(rows), encoding='utf-8')
    compiler = root / 'Library/UnityQGameFramework/Tools/Luban/5.1.0/Luban/Luban.dll'
    subprocess.run(['dotnet', str(compiler), '--conf', str(source / 'luban.conf'), '-t', 'client',
                    '--strict', '-c', 'cs-bin', '-d', 'bin', '--customTemplateDir',
                    str(root / 'Packages/com.zheliku.unityqgameframework/Editor/Luban/Templates~'),
                    '-x', 'outputCodeDir=' + str(output / 'Generated'),
                    '-x', 'outputDataDir=' + str(output / 'Data')], check=True)
    references = '\n'.join(
        f'<Reference Include={quoteattr(name)}><HintPath>{root / "Library/ScriptAssemblies" / (name + ".dll")}</HintPath></Reference>'
        for name in ('UnityQGameFramework.Core', 'UnityQGameFramework.Luban', 'UnityQGameFramework.Runtime'))
    sdk_major = int(subprocess.check_output(['dotnet', '--version'], text=True).split('.')[0])
    (output / 'Fixture.csproj').write_text(f'''<Project Sdk="Microsoft.NET.Sdk">
<PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net{sdk_major}.0</TargetFramework><UseAppHost>false</UseAppHost><UseSharedCompilation>false</UseSharedCompilation></PropertyGroup>
<ItemGroup>{references}</ItemGroup></Project>''', encoding='utf-8')
    (output / 'Program.cs').write_text('''using System;
using System.IO;
using System.Linq;
using QgfFixture;
using UnityQGameFramework.Core;
using UnityQGameFramework.Core.DataTable;
using UnityQGameFramework.Luban;
class Program
{
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    static void Main()
    {
        var tables = new Tables(name => new ByteBuf(File.ReadAllBytes("Data/" + name + ".bytes")));
        var manager = FrameworkEntry.GetModule<IDataTableManager>();
        manager.RegisterDataTables(tables.DataTables);
        Check(tables.DataTables.Count == 3, "Only int-Id class maps should become QGF views.");
        Check(ReferenceEquals(manager.GetDataTable<Item>().GetDataRow(1), tables.TbItem.Get(1)), "Native/QGF row identity");
        var named = tables.DataTables.Single(t => t.Type == typeof(Item) && t.Name != "");
        Check(manager.GetDataTable<Item>(named.Name).GetDataRow(2).Name == "Second", "Named table for the same bean");
        Check(ReferenceEquals(tables.TbUse.Get(10).ItemId_Ref, tables.TbItem.Get(1)), "Cross-table reference");
        Check(tables.TbItem.Get(1).Values.Count == 2, "Container deserialization");
        Check(tables.TbText.Get("hello").Name == "World", "String key native access");
        Check(tables.TbValue.Get(20).Amount == 42 && ((IDataRow)tables.TbValue.Get(20)).Id == 20, "Value-type row");
        Check(tables.TbGlobal.Title == "Fixture" && tables.TbList.DataList[0].Name == "List", "Singleton/list tables");
        FrameworkEntry.Shutdown();
        File.WriteAllText("result.json", "{\\"passed\\":true,\\"checks\\":8}");
        Console.WriteLine("Passed: cross-reference, native keys/containers/structs, named QGF views.");
    }
}''', encoding='utf-8')
    subprocess.run(['dotnet', 'run', '--project', str(output / 'Fixture.csproj'), '--configuration', 'Release'], cwd=output, check=True)


if __name__ == '__main__':
    main()
