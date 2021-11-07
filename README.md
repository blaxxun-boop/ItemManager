# Item Manager

Can be used to easily add new items to Valheim.

## How to add items

Copy the asset bundle into your project and make sure to set it as an EmbeddedResource in the properties of the asset bundle.
Default path for the asset bundle is an `assets` directory, but you can override this.

### Option 1: Copying the ItemManager.cs into your project

The easiest way is to simply copy the ItemManager.cs as a new class into your mod project.
Then add the following three lines to the bottom of the first PropertyGroup in your .csproj file, to enable C# V9.0 features and to allow the use of publicized DLLs.

```xml
<LangVersion>9</LangVersion>
<Nullable>enable</Nullable>
<AllowUnsafeBlocks>true</AllowUnsafeBlocks>
```

After that, simply add `using ItemManager;` to your mod and use the `Item` class, to add your items.

### Option 2: Merging the precompiled DLL into your mod

Download the ItemManager.dll from the release section to the right.
Including the dll is best done via ILRepack (https://github.com/ravibpatel/ILRepack.Lib.MSBuild.Task). You can load this package (ILRepack.Lib.MSBuild.Task) from NuGet.

If you have installed ILRepack via NuGet, simply create a file named `ILRepack.targets` in your project and copy the following content into the file

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <Target Name="ILRepacker" AfterTargets="Build">
        <ItemGroup>
            <InputAssemblies Include="$(TargetPath)" />
            <InputAssemblies Include="$(OutputPath)\ItemManager.dll" />
        </ItemGroup>
        <ILRepack Parallel="true" DebugInfo="true" Internalize="true" InputAssemblies="@(InputAssemblies)" OutputFile="$(TargetPath)" TargetKind="SameAsPrimaryAssembly" LibraryPath="$(OutputPath)" />
    </Target>
</Project>
```

Make sure to set the ItemManager.dll in your project to "Copy to output directory" in the properties of the dll and to add a reference to it.
After that, simply add `using ItemManager;` to your mod and use the `Item` class, to add your items.

## Example project

This adds three different weapons from two different asset bundles. The `ironfang` asset bundle is in a directory called `IronFang`, while the `heroset` asset bundle is in a directory called `assets`.

```csharp
using BepInEx;
using ItemManager;

namespace Weapons
{
	[BepInPlugin(ModGUID, ModName, ModVersion)]
	public class Weapons : BaseUnityPlugin
	{
		private const string ModName = "Weapons";
		private const string ModVersion = "1.0";
		private const string ModGUID = "org.bepinex.plugins.weapons";
		
		public void Awake()
		{
			Item ironFangAxe = new("ironfang", "IronFangAxe", "IronFang");
			ironFangAxe.Crafting.Add(CraftingTable.Forge, 3);
			ironFangAxe.RequiredItems.Add("Iron", 20);
			ironFangAxe.RequiredItems.Add("WolfFang", 10);
			ironFangAxe.RequiredItems.Add("Silver", 4);
			ironFangAxe.CraftAmount = 2; // We really want to dual wield these
			
			Item heroBlade = new("heroset", "HeroBlade");
			heroBlade.Crafting.Add(CraftingTable.Workbench, 2);
			heroBlade.RequiredItems.Add("Wood", 5);
			heroBlade.RequiredItems.Add("DeerHide", 2);
			
			Item heroShield = new("heroset", "HeroShield");
			heroShield.Crafting.Add(CraftingTable.Workbench, 1);
			heroShield.RequiredItems.Add("Wood", 10);
			heroShield.RequiredItems.Add("Flint", 5);
		}
	}
}

```
