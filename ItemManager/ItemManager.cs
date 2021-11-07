using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace ItemManager
{
	[PublicAPI]
	public enum CraftingTable
	{
		[InternalName("piece_workbench")] Workbench,
		[InternalName("piece_cauldron")] Cauldron,
		[InternalName("forge")] Forge,
		[InternalName("piece_artisanstation")] ArtisanTable,
		[InternalName("piece_stonecutter")] StoneCutter
	}

	public class InternalName : Attribute
	{
		public readonly string internalName;
		public InternalName(string internalName) => this.internalName = internalName;
	}
	
	[PublicAPI]
	public class RequiredResourceList
	{
		public readonly List<Requirement> Requirements = new();

		public void Add(string itemName, int amount) => Requirements.Add(new Requirement { itemName = itemName, amount = amount });
	}
	
	[PublicAPI]
	public class CraftingStationList
	{
		public readonly List<CraftingStationConfig> Stations = new();

		public void Add(CraftingTable table, int level) => Stations.Add(new CraftingStationConfig { Table = table, level = level });
	}

	public struct Requirement
	{
		public string itemName;
		public int amount;
	}

	public struct CraftingStationConfig
	{
		public CraftingTable Table;
		public int level;
	}

	[PublicAPI]
	public class Item
	{
		private static readonly List<Item> registeredItems = new();

		public readonly GameObject Prefab;

		/// <summary>
		/// Specifies the resources needed to craft the item.
		/// <para>Use .Add to add resources with their internal ID and an amount.</para>
		/// <para>Use one .Add for each resource type the item should need.</para>
		/// </summary>
		public readonly RequiredResourceList RequiredItems = new();
		/// <summary>
		/// Specifies the crafting station needed to craft the item.
		/// <para>Use .Add to add a crafting station, using the CraftingTable enum and a minimum level for the crafting station.</para>
		/// <para>Use one .Add for each crafting station.</para>
		/// </summary>
		public readonly CraftingStationList Crafting = new();
		/// <summary>
		/// Specifies the number of items that should be given to the player with a single craft of the item.
		/// <para>Defaults to 1.</para>
		/// </summary>
		public int CraftAmount = 1;

		public Item(string assetBundleFileName, string prefabName, string folderName = "assets") : this(PrefabManager.RegisterAssetBundle(assetBundleFileName, folderName), prefabName) { }

		public Item(AssetBundle bundle, string prefabName)
		{
			Prefab = PrefabManager.RegisterPrefab(bundle, prefabName);
			registeredItems.Add(this);
		}

		[HarmonyPriority(Priority.VeryHigh)]
		internal static void Patch_ObjectDBInit(ObjectDB __instance)
		{
			if (__instance.GetItemPrefab("Wood") == null)
			{
				return;
			}

			foreach (Item item in registeredItems)
			{
				foreach (CraftingStationConfig station in item.Crafting.Stations)
				{
					Recipe recipe = ScriptableObject.CreateInstance<Recipe>();
					recipe.name = $"{item.Prefab.name}_Recipe_{station.Table.ToString()}";
					recipe.m_amount = item.CraftAmount;
					recipe.m_enabled = true;
					recipe.m_item = item.Prefab.GetComponent<ItemDrop>();
					recipe.m_resources = item.RequiredItems.Requirements.Select(r => new Piece.Requirement { m_amount = r.amount, m_resItem = __instance.GetItemPrefab(r.itemName).GetComponent<ItemDrop>() }).ToArray();
					recipe.m_craftingStation = ZNetScene.instance.GetPrefab(((InternalName)typeof(CraftingTable).GetMember(station.Table.ToString())[0].GetCustomAttributes(typeof(InternalName)).First()).internalName).GetComponent<CraftingStation>();
					recipe.m_minStationLevel = station.level;

					__instance.m_recipes.Add(recipe);
				}
			}
		}
	}

	public static class PrefabManager
	{
		static PrefabManager()
		{
			Harmony harmony = new("org.bepinex.helpers.ItemManager");
			harmony.Patch(AccessTools.DeclaredMethod(typeof(ObjectDB), nameof(ObjectDB.CopyOtherDB)), new HarmonyMethod(AccessTools.DeclaredMethod(typeof(PrefabManager), nameof(Patch_ObjectDBInit))));
			harmony.Patch(AccessTools.DeclaredMethod(typeof(ObjectDB), nameof(ObjectDB.Awake)), new HarmonyMethod(AccessTools.DeclaredMethod(typeof(PrefabManager), nameof(Patch_ObjectDBInit))));
			harmony.Patch(AccessTools.DeclaredMethod(typeof(ObjectDB), nameof(ObjectDB.CopyOtherDB)), new HarmonyMethod(AccessTools.DeclaredMethod(typeof(Item), nameof(Item.Patch_ObjectDBInit))));
			harmony.Patch(AccessTools.DeclaredMethod(typeof(ObjectDB), nameof(ObjectDB.Awake)), new HarmonyMethod(AccessTools.DeclaredMethod(typeof(Item), nameof(Item.Patch_ObjectDBInit))));
		}
		
		private struct BundleId
		{
			[UsedImplicitly]
			public string assetBundleFileName;
			[UsedImplicitly]
			public string folderName;
		}

		private static readonly Dictionary<BundleId, AssetBundle> bundleCache = new();

		public static AssetBundle RegisterAssetBundle(string assetBundleFileName, string folderName = "assets")
		{
			BundleId id = new() { assetBundleFileName = assetBundleFileName, folderName = folderName };
			if (!bundleCache.TryGetValue(id, out AssetBundle assets))
			{
				assets = bundleCache[id] = AssetBundle.LoadFromStream(Assembly.GetExecutingAssembly().GetManifestResourceStream(Assembly.GetExecutingAssembly().GetName().Name + $".{folderName}." + assetBundleFileName));
			}
			return assets;
		}

		private static readonly List<GameObject> prefabs = new();

		public static GameObject RegisterPrefab(AssetBundle assets, string prefabName)
		{
			GameObject prefab = assets.LoadAsset<GameObject>(prefabName);

			prefabs.Add(prefab);

			return prefab;
		}

		[HarmonyPriority(Priority.VeryHigh)]
		private static void Patch_ObjectDBInit(ObjectDB __instance)
		{
			if (__instance.GetItemPrefab("Wood") == null)
			{
				return;
			}

			foreach (GameObject prefab in prefabs)
			{
				__instance.m_items.Add(prefab);
				ZNetScene.instance.m_prefabs.Add(prefab);
				ZNetScene.instance.m_namedPrefabs.Add(prefab.name.GetStableHashCode(), prefab);
			}
			__instance.UpdateItemHashes();
		}
	}
}
