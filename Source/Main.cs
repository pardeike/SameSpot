using Verse;
using Harmony;
using Verse.AI;
using RimWorld;
using System.Linq;
using System.IO;
using System.Reflection;

namespace SameSpot
{
	[StaticConstructorOnStartup]
	[HarmonyPatch]
	class Main
	{
		static Main()
		{
			var path = GetHarmonyConfigFile();
			var harmony = HarmonyInstance.RegisterWithFile(path);
			harmony.PatchAll(typeof(Main).Module);
		}

		static string GetHarmonyConfigFile()
		{
			var myModule = typeof(Main).Module;
			ModContentPack modInfo = LoadedModManager.RunningMods
				.FirstOrDefault(mcp => mcp.assemblies.loadedAssemblies
					.ToList().SelectMany(a => a.GetLoadedModules()).Contains(myModule));
			if (modInfo == null) throw new InvalidDataException("Cannot find mod for module " + myModule);
			var sep = Path.DirectorySeparatorChar;
			return string.Concat(new object[] { modInfo.RootDir, sep, "About", sep, "Harmony.txt" });
		}

		static MethodInfo TargetMethod()
		{
			var predicateClass = typeof(RCellFinder).GetNestedTypes(AccessTools.all)
				.FirstOrDefault(t => t.FullName.Contains("BestOrderedGotoDestNear"));
			if (predicateClass == null) return null;
			var method = predicateClass.GetMethods(AccessTools.all).FirstOrDefault(m => m.ReturnType == typeof(bool));
			if (method == null)
				Log.Warning("Error: Cannot find and patch BestOrderedGotoDestNear.Predicate. SameSpot mod won't work, sorry.");
			return method;
		}

		static void Postfix(object instance, ref bool result, ref IntVec3 cell)
		{
			if (result == false)
			{
				var selector = Find.Selector;
				var isSingleColonistSelected = (selector.SelectedObjects.Count == 1 && selector.SelectedObjects[0] is Pawn);
				if (isSingleColonistSelected)
				{
					var searcher = Traverse.Create(instance).Field("searcher").GetValue<Pawn>();
					result = searcher.CanReach(cell, PathEndMode.OnCell, Danger.Deadly, false, TraverseMode.ByPawn);
				}
			}
		}
	}
}