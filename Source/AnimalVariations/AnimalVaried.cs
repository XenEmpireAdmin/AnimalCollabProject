using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using System.Reflection;
using UnityEngine;
using System.Xml;
using System.Linq;

namespace AnimalVariations
{
	public class AnimalMultiSkins : Pawn
	{
		//
		// Fields and Properties
		//
		private static string assembly_name = Assembly.GetExecutingAssembly().GetName().Name;

		PawnRenderer pawn_renderer;
		public string male_graphic;
		public string female_graphic;

		private int skin_index = -1;
		private int winter_coat_timer;
		private const int winter_coat_trans_time = 50;
		private bool winterized;
		private int last_age_stage;

		internal bool benis = false;

		// holds the XML data
		internal static XmlDocument skinset_xml = null;

		//
		// Methods
		//

		// Saves and loads the data
		public override void ExposeData ()
		{
			Scribe_Values.Look<int> (ref skin_index, "skin_index", -1, false);
			Scribe_Values.Look<int> (ref winter_coat_timer, "winter_coat_timer", 0, false);
			Scribe_Values.Look<bool> (ref winterized, "winterized", false, false);
			base.ExposeData ();
		}

		public bool DoesTexSetExist(string path){
			if (ContentFinder<Texture2D>.Get (path + "_side", false) != null &&
				ContentFinder<Texture2D>.Get (path + "_front", false) != null &&
			   ContentFinder<Texture2D>.Get (path + "_back", false) != null) {
				return true;
			}
			return false;
		}

		public int num_skins (int lifeStageIndex, bool female=false) {
			TryLoadSkinSet ();
			if (skinset_xml == null) {
				Log.Warning ("Couldn't find requested " + def.defName + "_SkinSet.xml");
				return 0;
			}
			string fem = "";
			if (female)
				fem = "Female";

			// Check if the first lifeStage element applies to all
			if (skinset_xml.SelectNodes ("SkinSet/lifeStage").Count > 0) {
				if (skinset_xml.SelectSingleNode ("SkinSet/lifeStage").SelectNodes ("./appliesToAll").Count > 0)
					if (skinset_xml.SelectSingleNode ("SkinSet/lifeStage").SelectSingleNode ("./appliesToAll").InnerText == "true")
						lifeStageIndex = 0;
			}

			// Make sure lifeStage elements exist
			if (skinset_xml.SelectNodes ("SkinSet/lifeStage").Count >= lifeStageIndex + 1) {
				// Make sure the variants* element exists in the current lifeStage element
				if (skinset_xml.SelectNodes("SkinSet/lifeStage")[lifeStageIndex].SelectNodes("./variants"+fem).Count > 0){
					// Make sure the current lifeStage/variants* element has fields
					if (skinset_xml.SelectNodes ("SkinSet/lifeStage") [lifeStageIndex].SelectSingleNode ("./variants" + fem).HasChildNodes) {
						// Return the numbers of child node
						return skinset_xml.SelectNodes ("SkinSet/lifeStage")[lifeStageIndex].SelectSingleNode("./variants" + fem).SelectNodes ("./skin").Count;
					} else {
						if(benis)
							Log.Warning ("variants" + fem + " element has no \"skin\" elements for "+def.defName+"'s lifeStageIndex of "+lifeStageIndex);
					}
				} else {
					if(benis)
						Log.Warning ("Couldn't find variants"+fem+" element for "+def.defName+"'s lifeStageIndex of "+lifeStageIndex);
				}
			} else{
				if(benis)
					Log.Warning ("Couldn't find lifeStage element(s) for "+def.defName);
			}

			return 0;
		}

		public bool TryLoadSkinSet(){
			// the xml is already loaded
			if (skinset_xml != null) {
				string xml_owner = skinset_xml.BaseURI.Substring (skinset_xml.BaseURI.LastIndexOf ('/') + 1, skinset_xml.BaseURI.Length - (skinset_xml.BaseURI.LastIndexOf ('/') + 1) - "_SkinSet.xml".Length);
				// The currently loaded xml is the same as the one being requested
				if(xml_owner == def.defName)
					return true;
			}

			// loading SkinSet
			string tex_path = kindDef.lifeStages [kindDef.lifeStages.Count -1].bodyGraphicData.texPath;
			string xmlpath = "";
			bool found = false;

			foreach (ModMetaData mod in ModLister.AllInstalledMods.Where(m => m.Active)) {
				xmlpath = mod.RootDir + "/Textures/" + tex_path.Substring(0,tex_path.LastIndexOf('/') + 1) + def.defName + "_SkinSet.xml";
				if (System.IO.File.Exists (xmlpath)) {
					skinset_xml = new XmlDocument ();
					skinset_xml.Load (xmlpath);
					found = true;
					break;
				}
			}
			if (found){
				if (Prefs.DevMode) Log.Message ("["+assembly_name+"] Loading XML data from: " + xmlpath);
				return true;
			} else {
				skinset_xml = null;
				return false;
			}
		}

		private XmlNode GetSkinNode(int lifeStage, int index, bool female=false){
			if (skinset_xml == null)
				return null;

			string elementName = "variants";
			if (female)
				elementName += "Female";

			// Check if this life stage "appliesToAll" life stages
			if (skinset_xml.SelectSingleNode ("SkinSet/lifeStage").SelectNodes ("./appliesToAll").Count > 0) {
				if (skinset_xml.SelectSingleNode ("SkinSet/lifeStage").SelectSingleNode ("./appliesToAll").InnerText == "true")
					lifeStage = 0;
			}
			// Make sure up to the current lifeStage exists
			if (skinset_xml.SelectNodes ("SkinSet/lifeStage").Count < lifeStage + 1)
				return null;

			// Make sure the current lifeStage element has a variants* element
			if (skinset_xml.SelectNodes("SkinSet/lifeStage")[lifeStage].SelectNodes("./"+ elementName).Count == 0)
				return null;
			if (num_skins(lifeStage, female) == 0 || index < 1)
				return null;
				
			XmlNodeList skins = skinset_xml.DocumentElement.GetElementsByTagName (elementName) [0].ChildNodes;
			return skins[index - 1];
		}

		private string GetSkinValue(int lifeStage, int index, string key, bool female=false){
			if (skinset_xml == null) {
				return null;
			}

			string result = null;
			if (index != 0) {
				XmlNode skin = GetSkinNode (lifeStage, index, female);
				// return the requested variable
				foreach (XmlNode node in skin.ChildNodes) {
					if (node.Name == key) {
						result = node.InnerText;
					}
				}
			}

			// Default values
			if (result == null) {
				Dictionary<string, string> defaults = new Dictionary<string, string>{
					{"color", "(255,255,255)"},
					{"color2", "(255,255,255)"},
					{"scale", "1"},
					{"useComplexShader", "false"},
					{"commonality", "1"}
				};
				if (defaults.ContainsKey (key)) {
					result = defaults [key];
				}
			}
			return result;
		}
		private XmlNode GetWinterCoatNode(int lifeStage){
			if (skinset_xml == null) {
				return null;
			}
			// Check if this life stage "appliesToAll" life stages
			if (skinset_xml.SelectSingleNode ("SkinSet/lifeStage").SelectNodes ("./appliesToAll").Count > 0) {
				if (skinset_xml.SelectSingleNode ("SkinSet/lifeStage").SelectSingleNode ("./appliesToAll").InnerText == "true")
					lifeStage = 0;
			}

			if (skinset_xml.SelectNodes ("SkinSet/lifeStage").Count < lifeStage + 1)
				return null;
			if (skinset_xml.SelectNodes ("SkinSet/lifeStage")[lifeStage].SelectNodes("./winterCoat").Count == 0)
				return null;
			return skinset_xml.SelectNodes ("SkinSet/lifeStage")[lifeStage].SelectSingleNode("./winterCoat");
		}
		private string GetWinterCoatValue(int lifeStage, string key){
			if (GetWinterCoatNode (lifeStage) == null) {
				return null;
			}
			string result = null;
			if (GetWinterCoatNode (lifeStage).SelectNodes ("./"+key).Count > 0) {
				result = GetWinterCoatNode (lifeStage).SelectSingleNode ("./"+key).InnerText;
			}

			// Default values
			if (result == null) {
				Dictionary<string, string> defaults = new Dictionary<string, string> {
					{ "color", "(255,255,255)" },
					{ "color2", "(255,255,255)" },
					{ "scale", "1" },
					{ "useComplexShader", "false" },
					{ "commonality", "1" }
				};
				if (defaults.ContainsKey (key)) {
					result = defaults [key];
				}
			}
			return result;
		}
		private bool UsesWinterCoat(int lifeStage){
			// Check if there is a declared value
			if (GetWinterCoatValue (lifeStage, "useWinterCoat") != null)
				return bool.Parse (GetWinterCoatValue (lifeStage, "useWinterCoat"));
			// no declared value defaults to false
			return false;
		}

		private static Color ParseXMLColor(string color){
			string[] rgb = color.Trim (new char[] {'(', ')' }).Split (new char[] {','});
			return GenColor.FromBytes (int.Parse(rgb [0]), int.Parse(rgb [1]), int.Parse(rgb [2]));
		}

		public bool ApplyNewSkin (int index){
			// male
			string base_graphic_path = male_graphic;
			bool female = false;
			if (this.gender == Gender.Female && female_graphic != null) {
				female = true;
				base_graphic_path = female_graphic;
			}

			TryLoadSkinSet ();

			// Throw an error when index out of range
			if (index > num_skins(ageTracker.CurLifeStageIndex, female)) {
				Log.Error ("Error in " + assembly_name + ": AnimalVaried.ApplyNewSkin tried to set skin index out of available range for "+def.defName);
				return false;
			}

			Vector2 draw_size = ageTracker.CurKindLifeStage.bodyGraphicData.drawSize;
			Color draw_color = Color.white;
			Color draw_color2 = Color.white;
			Shader shader = ShaderDatabase.Cutout;

			int ageInd = ageTracker.CurLifeStageIndex;

			// Use the winter coat instead
			if ((winter_coat_timer == winter_coat_trans_time || winterized) && UsesWinterCoat(ageTracker.CurLifeStageIndex)) {
				// 
				string winter_graphic = base_graphic_path;

				// Look for a custom winter graphic
				// For males
				if (!female) {
					if (GetWinterCoatValue (ageInd, "texName") != null) {
						winter_graphic = base_graphic_path.Substring(0,base_graphic_path.LastIndexOf('/') + 1) + GetWinterCoatValue (ageInd, "texName");
					} else {
						// Look for the default name instead
						if (DoesTexSetExist (base_graphic_path + "_winter"))
							winter_graphic = base_graphic_path + "_winter";
					}
				}
				// For females
				else {
					if (GetWinterCoatValue (ageInd, "texNameFemale") != null) {
						winter_graphic = base_graphic_path.Substring(0,base_graphic_path.LastIndexOf('/') +1) + GetWinterCoatValue (ageInd, "texNameFemale");
					} else {
						// Look for the default name instead
						if (DoesTexSetExist (base_graphic_path + "_winter"))
							winter_graphic = base_graphic_path + "_winter";
					}
				}

				if (bool.Parse(GetWinterCoatValue (ageInd, "useComplexShader"))) {
					shader = ShaderDatabase.CutoutComplex;
				}
				draw_color = ParseXMLColor(GetWinterCoatValue (ageInd, "color"));
				draw_color2 = ParseXMLColor(GetWinterCoatValue (ageInd, "color2"));

				draw_size *= float.Parse (GetWinterCoatValue (ageInd, "scale"));

				//pawn_renderer.graphics.ClearCache();
				pawn_renderer.graphics.nakedGraphic = GraphicDatabase.Get<Graphic_Multi> (winter_graphic, shader, draw_size, draw_color, draw_color2);
				skin_index = index;
				ReloadGraphicData ();
				return true;
			}

			// Use the default skin
			if (index == 0) {
				// Get default values
				if (ageTracker.CurKindLifeStage.bodyGraphicData != null) {
					draw_color = ageTracker.CurKindLifeStage.bodyGraphicData.color;
					draw_color2 = ageTracker.CurKindLifeStage.bodyGraphicData.colorTwo;
					if (this.gender == Gender.Female && ageTracker.CurKindLifeStage.femaleGraphicData != null) {
						draw_size = ageTracker.CurKindLifeStage.femaleGraphicData.drawSize;
						draw_color = ageTracker.CurKindLifeStage.femaleGraphicData.color;
						draw_color2 = ageTracker.CurKindLifeStage.bodyGraphicData.colorTwo;
					}
				}
				// Get default shader type
				GraphicData gdata = ageTracker.CurKindLifeStage.bodyGraphicData;
				if(gender == Gender.Female && ageTracker.CurKindLifeStage.femaleGraphicData != null)
					gdata = ageTracker.CurKindLifeStage.femaleGraphicData;
				if (gdata.shaderType == ShaderType.CutoutComplex)
					shader = ShaderDatabase.CutoutComplex;


				//pawn_renderer.graphics.ClearCache();
				pawn_renderer.graphics.nakedGraphic = GraphicDatabase.Get<Graphic_Multi> (base_graphic_path, shader, draw_size, draw_color, draw_color2);
				ReloadGraphicData ();
				skin_index = 0;
				return true;
			}

			// Use a variant skin
			if (GetSkinNode(ageInd, index, female) != null && index != 0) {
				draw_color = ParseXMLColor (GetSkinValue (ageInd, index, "color", female));
				draw_color2 = ParseXMLColor (GetSkinValue (ageInd, index, "color2", female));

				XmlNode skin_node = GetSkinNode(ageInd, index, female);
				foreach (XmlNode node in skin_node.ChildNodes) {
					// Get colors
					if (node.Name == "color" || node.Name == "colour")
						draw_color = ParseXMLColor (node.InnerText);

					if (node.Name == "color2" || node.Name == "colour2")
						draw_color2 = ParseXMLColor (node.InnerText);
				}
			}

			if (GetSkinValue (ageInd, index, "useComplexShader", female) == "true")
				shader = ShaderDatabase.CutoutComplex;

			// Set the scale of the skin
			draw_size *= float.Parse (GetSkinValue (ageInd, index, "scale", female));

			// Set a new skin
			string texSetPath = base_graphic_path;
			if (GetSkinValue (ageInd, index, "texName", female) == null) {
				texSetPath = base_graphic_path;
			} else
				texSetPath = texSetPath.Substring (0, texSetPath.LastIndexOf ('/') + 1)+ GetSkinValue (ageInd, index, "texName", female);

			//pawn_renderer.graphics.ClearCache();
			Graphic_Multi new_graphic = (Graphic_Multi)GraphicDatabase.Get<Graphic_Multi> (texSetPath, shader, draw_size, draw_color, draw_color2);
			pawn_renderer.graphics.nakedGraphic = new_graphic;
			skin_index = index;
			ReloadGraphicData ();
			return true;
		}

		private void ReloadGraphicData()
		{
			GraphicData rData = pawn_renderer.graphics.nakedGraphic.data = new GraphicData();
			rData.shadowData = ageTracker.CurKindLifeStage.bodyGraphicData.shadowData;
		}


		public override void TickRare ()
		{
			if (Map != null) {
				// Process the winter coat ticker
				if (Map.mapTemperature.OutdoorTemp < 0 && winter_coat_timer < winter_coat_trans_time) {
					winter_coat_timer++;
					//Log.Message ("winter_coat_timer at " + winter_coat_timer);
					if (winter_coat_timer == winter_coat_trans_time) {
						//Log.Message ("Winter coat applied");
						winterized = true;
						ApplyNewSkin (skin_index);
					}
				}
				if (Map.mapTemperature.OutdoorTemp > 0 && winter_coat_timer > 0) {
					winter_coat_timer--;
					//Log.Message ("winter_coat_timer at " + winter_coat_timer);
					if (winter_coat_timer == 0) {
						//Log.Message ("Winter coat removed");
						winterized = false;
						ApplyNewSkin (skin_index);
					}
				}
			}

			base.TickRare ();
		}

		public override void Tick ()
		{
			base.Tick ();

			// age transition occurred?
			if (ageTracker.CurLifeStageIndex != last_age_stage && skin_index != -1) {
				last_age_stage = ageTracker.CurLifeStageIndex;
				LongEventHandler.ExecuteWhenFinished (delegate {
					//Log.Message("Age transition occurred");
					ApplyNewSkin (skin_index);
				});
			}
		}

		// Doesn't seem to be able to get relatives on pawn spawn. fug
		private void CheckRelatives ()
		{
			// Potentially find mother and father
			Pawn mother = null;
			Pawn father = null;
			// Don't bother running this if we have no relatives
			if (relations.DirectRelations.Count > 0) {
				foreach (DirectPawnRelation relative in relations.DirectRelations) {
					if (relative.def == PawnRelationDefOf.Parent) {
						if (relative.otherPawn.gender == Gender.Female)
							mother = relative.otherPawn;
						else if (relative.otherPawn.gender == Gender.Male)
							father = relative.otherPawn;
					}
				}
			} else if (Prefs.DevMode) {
				Log.Warning ("No relations found!");
			}

			// Define a new skin
			int new_skin = 0;
			if (mother != null) {
				if (father != null) {
					// Skin is randomly mother or father's
					new_skin = ((AnimalMultiSkins)(new List<Pawn>{ mother, father }.RandomElement ())).skin_index;
				} else
					// No father; skin is mother's
					new_skin = ((AnimalMultiSkins)mother).skin_index;
			} else {
				// No parents at all? Randomly selected skin
				Log.Message ("No mother found");
				return;
			}
			ApplyNewSkin (new_skin);
		}

		public override void SpawnSetup (Map map, bool respawningAfterLoad)
		//public override void SpawnSetup (Map map)
		{
			base.SpawnSetup (map, respawningAfterLoad);

			male_graphic = ageTracker.CurKindLifeStage.bodyGraphicData.texPath;
			bool female = false;
			if (ageTracker.CurKindLifeStage.femaleGraphicData != null) {
				female_graphic = ageTracker.CurKindLifeStage.femaleGraphicData.texPath;
				female |= gender == Gender.Female;
			}
			pawn_renderer = ((Pawn_DrawTracker)(typeof(Pawn).GetField ("drawer", BindingFlags.Instance | BindingFlags.NonPublic).GetValue (this))).renderer;

			if (TryLoadSkinSet () == false) {
				Log.Error ("Couldn't find " + def.defName + "_SkinSet.xml! This file is required for animals using the variations framework.");
				return;
			}

			int ageInd = ageTracker.CurLifeStageIndex;

			// No skin set
			if (skin_index == -1) {
				// Have animals start with winter coat if outside temperature is below freezing on initial spawn
				if (Map.mapTemperature.OutdoorTemp < 0)
					winter_coat_timer = winter_coat_trans_time;

				int new_skin = Rand.Range (0, num_skins(ageInd, female) + 1);
				if(skinset_xml != null && new_skin != 0){

					int tries = 100;
					float chance = float.Parse (GetSkinValue (ageInd, new_skin, "commonality", female));
					while (chance <= Rand.Value && tries > 0) {
						new_skin = Rand.Range (0, num_skins(ageInd, female) + 1);
						if (new_skin == 0)
							chance = 1; // base graphic 'skin' always has commonality of 1. So end the loop
						else
							// Get the probability of the newly selected skin
							chance = float.Parse (GetSkinValue (ageInd, new_skin, "commonality", female));
						tries--;
					}

					// This probably shouldn't happen, but just in case
					if (tries == 0) {
						new_skin = 0;
					}
				}

				int skinChance = (int)((float.Parse(GetSkinValue (ageInd, new_skin, "commonality", female))/(num_skins(ageInd, female) + 1)) * 100);
				Log.Message ("Creating new "+gender.ToString().ToLower() +" "+def.defName+" with skin index of " + new_skin +" where "+num_skins(ageInd, female)+" possible variants were available. Chance was "+ skinChance +"%.");
				LongEventHandler.ExecuteWhenFinished (delegate {
					ApplyNewSkin (new_skin);
				});
				last_age_stage = ageTracker.CurLifeStageIndex;
			}
			else {
				LongEventHandler.ExecuteWhenFinished (delegate {
					ApplyNewSkin (skin_index);
				});
			}

		}
	}
}

