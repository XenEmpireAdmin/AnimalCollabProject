using RimWorld;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;
using UnityEngine;
using Verse;

namespace AnimalVariations
{
    public class AnimalMultiSkins : Pawn
    {
        private static string assembly_name = Assembly.GetExecutingAssembly().GetName().Name;

        private PawnRenderer pawn_renderer;

        public string male_graphic;

        public string female_graphic;

        private int skin_index = -1;

        private int winter_coat_timer;

        private const int winter_coat_trans_time = 50;

        private bool winterized;

        private int last_age_stage;

        internal bool benis = false;

        internal static XmlDocument skinset_xml = null;

        public override void ExposeData()
        {
            Scribe_Values.Look<int>(ref this.skin_index, "skin_index", -1, false);
            Scribe_Values.Look<int>(ref this.winter_coat_timer, "winter_coat_timer", 0, false);
            Scribe_Values.Look<bool>(ref this.winterized, "winterized", false, false);
            base.ExposeData();
        }

        public bool DoesTexSetExist(string path)
        {
            return ContentFinder<Texture2D>.Get(path + "_side", false) != null && ContentFinder<Texture2D>.Get(path + "_front", false) != null && ContentFinder<Texture2D>.Get(path + "_back", false) != null;
        }

        public int num_skins(int lifeStageIndex, bool female = false)
        {
            this.TryLoadSkinSet();
            int result;
            if (AnimalMultiSkins.skinset_xml == null)
            {
                Log.Warning("Couldn't find requested " + this.def.defName + "_SkinSet.xml");
                result = 0;
            }
            else
            {
                string text = "";
                if (female)
                {
                    text = "Female";
                }
                if (AnimalMultiSkins.skinset_xml.SelectNodes("SkinSet/lifeStage").Count > 0)
                {
                    if (AnimalMultiSkins.skinset_xml.SelectSingleNode("SkinSet/lifeStage").SelectNodes("./appliesToAll").Count > 0 && AnimalMultiSkins.skinset_xml.SelectSingleNode("SkinSet/lifeStage").SelectSingleNode("./appliesToAll").InnerText == "true")
                    {
                        lifeStageIndex = 0;
                    }
                }
                if (AnimalMultiSkins.skinset_xml.SelectNodes("SkinSet/lifeStage").Count >= lifeStageIndex + 1)
                {
                    if (AnimalMultiSkins.skinset_xml.SelectNodes("SkinSet/lifeStage")[lifeStageIndex].SelectNodes("./variants" + text).Count > 0)
                    {
                        if (AnimalMultiSkins.skinset_xml.SelectNodes("SkinSet/lifeStage")[lifeStageIndex].SelectSingleNode("./variants" + text).HasChildNodes)
                        {
                            result = AnimalMultiSkins.skinset_xml.SelectNodes("SkinSet/lifeStage")[lifeStageIndex].SelectSingleNode("./variants" + text).SelectNodes("./skin").Count;
                            return result;
                        }
                        if (this.benis)
                        {
                            Log.Warning(string.Concat(new object[]
                            {
                                "variants",
                                text,
                                " element has no \"skin\" elements for ",
                                this.def.defName,
                                "'s lifeStageIndex of ",
                                lifeStageIndex
                            }));
                        }
                    }
                    else if (this.benis)
                    {
                        Log.Warning(string.Concat(new object[]
                        {
                            "Couldn't find variants",
                            text,
                            " element for ",
                            this.def.defName,
                            "'s lifeStageIndex of ",
                            lifeStageIndex
                        }));
                    }
                }
                else if (this.benis)
                {
                    Log.Warning("Couldn't find lifeStage element(s) for " + this.def.defName);
                }
                result = 0;
            }
            return result;
        }

        public bool TryLoadSkinSet()
        {
            bool result;
            if (AnimalMultiSkins.skinset_xml != null)
            {
                string a = AnimalMultiSkins.skinset_xml.BaseURI.Substring(AnimalMultiSkins.skinset_xml.BaseURI.LastIndexOf('/') + 1, AnimalMultiSkins.skinset_xml.BaseURI.Length - (AnimalMultiSkins.skinset_xml.BaseURI.LastIndexOf('/') + 1) - "_SkinSet.xml".Length);
                if (a == this.def.defName)
                {
                    result = true;
                    return result;
                }
            }
            string text = "";
            ModMetaData modMetaData = null;
            foreach (ModMetaData current in ModLister.AllInstalledMods)
            {
                if (current.Active && current.Name == "AnimalCollabProj")
                {
                    modMetaData = current;
                }
            }
            if (modMetaData != null)
            {
                string texPath = this.kindDef.lifeStages[this.kindDef.lifeStages.Count - 1].bodyGraphicData.texPath;
                text = string.Concat(new string[]
                {
                    modMetaData.RootDir.ToString(),
                    "/Textures/",
                    texPath.Substring(0, texPath.LastIndexOf('/') + 1),
                    this.def.defName,
                    "_SkinSet.xml"
                });
                Log.Message(text);
            }
            if (File.Exists(text))
            {
                AnimalMultiSkins.skinset_xml = new XmlDocument();
                AnimalMultiSkins.skinset_xml.Load(text);
                result = true;
            }
            else
            {
                AnimalMultiSkins.skinset_xml = null;
                result = false;
            }
            return result;
        }

        private XmlNode GetSkinNode(int lifeStage, int index, bool female = false)
        {
            XmlNode result;
            if (AnimalMultiSkins.skinset_xml == null)
            {
                result = null;
            }
            else
            {
                string text = "variants";
                if (female)
                {
                    text += "Female";
                }
                if (AnimalMultiSkins.skinset_xml.SelectSingleNode("SkinSet/lifeStage").SelectNodes("./appliesToAll").Count > 0)
                {
                    if (AnimalMultiSkins.skinset_xml.SelectSingleNode("SkinSet/lifeStage").SelectSingleNode("./appliesToAll").InnerText == "true")
                    {
                        lifeStage = 0;
                    }
                }
                if (AnimalMultiSkins.skinset_xml.SelectNodes("SkinSet/lifeStage").Count < lifeStage + 1)
                {
                    result = null;
                }
                else if (AnimalMultiSkins.skinset_xml.SelectNodes("SkinSet/lifeStage")[lifeStage].SelectNodes("./" + text).Count == 0)
                {
                    result = null;
                }
                else if (this.num_skins(lifeStage, female) == 0 || index < 1)
                {
                    result = null;
                }
                else
                {
                    XmlNodeList childNodes = AnimalMultiSkins.skinset_xml.DocumentElement.GetElementsByTagName(text)[0].ChildNodes;
                    result = childNodes[index - 1];
                }
            }
            return result;
        }

        private string GetSkinValue(int lifeStage, int index, string key, bool female = false)
        {
            string result;
            if (AnimalMultiSkins.skinset_xml == null)
            {
                result = null;
            }
            else
            {
                string text = null;
                if (index != 0)
                {
                    XmlNode skinNode = this.GetSkinNode(lifeStage, index, female);
                    IEnumerator enumerator = skinNode.ChildNodes.GetEnumerator();
                    try
                    {
                        while (enumerator.MoveNext())
                        {
                            XmlNode xmlNode = (XmlNode)enumerator.Current;
                            if (xmlNode.Name == key)
                            {
                                text = xmlNode.InnerText;
                            }
                        }
                    }
                    finally
                    {
                        IDisposable disposable;
                        if ((disposable = (enumerator as IDisposable)) != null)
                        {
                            disposable.Dispose();
                        }
                    }
                }
                if (text == null)
                {
                    Dictionary<string, string> dictionary = new Dictionary<string, string>
                    {
                        {
                            "color",
                            "(255,255,255)"
                        },
                        {
                            "color2",
                            "(255,255,255)"
                        },
                        {
                            "scale",
                            "1"
                        },
                        {
                            "useComplexShader",
                            "false"
                        },
                        {
                            "commonality",
                            "1"
                        }
                    };
                    if (dictionary.ContainsKey(key))
                    {
                        text = dictionary[key];
                    }
                }
                result = text;
            }
            return result;
        }

        private XmlNode GetWinterCoatNode(int lifeStage)
        {
            XmlNode result;
            if (AnimalMultiSkins.skinset_xml == null)
            {
                result = null;
            }
            else
            {
                if (AnimalMultiSkins.skinset_xml.SelectSingleNode("SkinSet/lifeStage").SelectNodes("./appliesToAll").Count > 0)
                {
                    if (AnimalMultiSkins.skinset_xml.SelectSingleNode("SkinSet/lifeStage").SelectSingleNode("./appliesToAll").InnerText == "true")
                    {
                        lifeStage = 0;
                    }
                }
                if (AnimalMultiSkins.skinset_xml.SelectNodes("SkinSet/lifeStage").Count < lifeStage + 1)
                {
                    result = null;
                }
                else if (AnimalMultiSkins.skinset_xml.SelectNodes("SkinSet/lifeStage")[lifeStage].SelectNodes("./winterCoat").Count == 0)
                {
                    result = null;
                }
                else
                {
                    result = AnimalMultiSkins.skinset_xml.SelectNodes("SkinSet/lifeStage")[lifeStage].SelectSingleNode("./winterCoat");
                }
            }
            return result;
        }

        private string GetWinterCoatValue(int lifeStage, string key)
        {
            string result;
            if (this.GetWinterCoatNode(lifeStage) == null)
            {
                result = null;
            }
            else
            {
                string text = null;
                if (this.GetWinterCoatNode(lifeStage).SelectNodes("./" + key).Count > 0)
                {
                    text = this.GetWinterCoatNode(lifeStage).SelectSingleNode("./" + key).InnerText;
                }
                if (text == null)
                {
                    Dictionary<string, string> dictionary = new Dictionary<string, string>
                    {
                        {
                            "color",
                            "(255,255,255)"
                        },
                        {
                            "color2",
                            "(255,255,255)"
                        },
                        {
                            "scale",
                            "1"
                        },
                        {
                            "useComplexShader",
                            "false"
                        },
                        {
                            "commonality",
                            "1"
                        }
                    };
                    if (dictionary.ContainsKey(key))
                    {
                        text = dictionary[key];
                    }
                }
                result = text;
            }
            return result;
        }

        private bool UsesWinterCoat(int lifeStage)
        {
            return this.GetWinterCoatValue(lifeStage, "useWinterCoat") != null && bool.Parse(this.GetWinterCoatValue(lifeStage, "useWinterCoat"));
        }

        private static Color ParseXMLColor(string color)
        {
            string[] array = color.Trim(new char[]
            {
                '(',
                ')'
            }).Split(new char[]
            {
                ','
            });
            return GenColor.FromBytes(int.Parse(array[0]), int.Parse(array[1]), int.Parse(array[2]), 255);
        }

        public bool ApplyNewSkin(int index)
        {
            string text = this.male_graphic;
            bool flag = false;
            if (this.gender == Gender.Female && this.female_graphic != null)
            {
                flag = true;
                text = this.female_graphic;
            }
            this.TryLoadSkinSet();
            bool result;
            if (index > this.num_skins(this.ageTracker.CurLifeStageIndex, flag))
            {
                Log.Error("Error in " + AnimalMultiSkins.assembly_name + ": AnimalVaried.ApplyNewSkin tried to set skin index out of available range for " + this.def.defName);
                result = false;
            }
            else
            {
                Vector2 vector = this.ageTracker.CurKindLifeStage.bodyGraphicData.drawSize;
                Color color = Color.white;
                Color color2 = Color.white;
                Shader shader = ShaderDatabase.Cutout;
                int curLifeStageIndex = this.ageTracker.CurLifeStageIndex;
                if ((this.winter_coat_timer == 50 || this.winterized) && this.UsesWinterCoat(this.ageTracker.CurLifeStageIndex))
                {
                    string text2 = text;
                    if (!flag)
                    {
                        if (this.GetWinterCoatValue(curLifeStageIndex, "texName") != null)
                        {
                            text2 = text.Substring(0, text.LastIndexOf('/') + 1) + this.GetWinterCoatValue(curLifeStageIndex, "texName");
                        }
                        else if (this.DoesTexSetExist(text + "_winter"))
                        {
                            text2 = text + "_winter";
                        }
                    }
                    else if (this.GetWinterCoatValue(curLifeStageIndex, "texNameFemale") != null)
                    {
                        text2 = text.Substring(0, text.LastIndexOf('/') + 1) + this.GetWinterCoatValue(curLifeStageIndex, "texNameFemale");
                    }
                    else if (this.DoesTexSetExist(text + "_winter"))
                    {
                        text2 = text + "_winter";
                    }
                    if (bool.Parse(this.GetWinterCoatValue(curLifeStageIndex, "useComplexShader")))
                    {
                        shader = ShaderDatabase.CutoutComplex;
                    }
                    color = AnimalMultiSkins.ParseXMLColor(this.GetWinterCoatValue(curLifeStageIndex, "color"));
                    color2 = AnimalMultiSkins.ParseXMLColor(this.GetWinterCoatValue(curLifeStageIndex, "color2"));
                    vector *= float.Parse(this.GetWinterCoatValue(curLifeStageIndex, "scale"));
                    this.pawn_renderer.graphics.ClearCache();
                    this.pawn_renderer.graphics.nakedGraphic = GraphicDatabase.Get<Graphic_Multi>(text2, shader, vector, color, color2);
                    this.skin_index = index;
                    result = true;
                }
                else if (index == 0)
                {
                    if (this.ageTracker.CurKindLifeStage.bodyGraphicData != null)
                    {
                        color = this.ageTracker.CurKindLifeStage.bodyGraphicData.color;
                        color2 = this.ageTracker.CurKindLifeStage.bodyGraphicData.colorTwo;
                        if (this.gender == Gender.Female && this.ageTracker.CurKindLifeStage.femaleGraphicData != null)
                        {
                            vector = this.ageTracker.CurKindLifeStage.femaleGraphicData.drawSize;
                            color = this.ageTracker.CurKindLifeStage.femaleGraphicData.color;
                            color2 = this.ageTracker.CurKindLifeStage.bodyGraphicData.colorTwo;
                        }
                    }
                    GraphicData graphicData = this.ageTracker.CurKindLifeStage.bodyGraphicData;
                    if (this.gender == Gender.Female && this.ageTracker.CurKindLifeStage.femaleGraphicData != null)
                    {
                        graphicData = this.ageTracker.CurKindLifeStage.femaleGraphicData;
                    }
                    if (graphicData.shaderType == ShaderType.CutoutComplex)
                    {
                        shader = ShaderDatabase.CutoutComplex;
                    }
                    this.pawn_renderer.graphics.ClearCache();
                    this.pawn_renderer.graphics.nakedGraphic = GraphicDatabase.Get<Graphic_Multi>(text, shader, vector, color, color2);
                    this.skin_index = 0;
                    result = true;
                }
                else
                {
                    if (this.GetSkinNode(curLifeStageIndex, index, flag) != null && index != 0)
                    {
                        color = AnimalMultiSkins.ParseXMLColor(this.GetSkinValue(curLifeStageIndex, index, "color", flag));
                        color2 = AnimalMultiSkins.ParseXMLColor(this.GetSkinValue(curLifeStageIndex, index, "color2", flag));
                        XmlNode skinNode = this.GetSkinNode(curLifeStageIndex, index, flag);
                        IEnumerator enumerator = skinNode.ChildNodes.GetEnumerator();
                        try
                        {
                            while (enumerator.MoveNext())
                            {
                                XmlNode xmlNode = (XmlNode)enumerator.Current;
                                if (xmlNode.Name == "color" || xmlNode.Name == "colour")
                                {
                                    color = AnimalMultiSkins.ParseXMLColor(xmlNode.InnerText);
                                }
                                if (xmlNode.Name == "color2" || xmlNode.Name == "colour2")
                                {
                                    color2 = AnimalMultiSkins.ParseXMLColor(xmlNode.InnerText);
                                }
                            }
                        }
                        finally
                        {
                            IDisposable disposable;
                            if ((disposable = (enumerator as IDisposable)) != null)
                            {
                                disposable.Dispose();
                            }
                        }
                    }
                    if (this.GetSkinValue(curLifeStageIndex, index, "useComplexShader", flag) == "true")
                    {
                        shader = ShaderDatabase.CutoutComplex;
                    }
                    vector *= float.Parse(this.GetSkinValue(curLifeStageIndex, index, "scale", flag));
                    string text3 = text;
                    if (this.GetSkinValue(curLifeStageIndex, index, "texName", flag) == null)
                    {
                        text3 = text;
                    }
                    else
                    {
                        text3 = text3.Substring(0, text3.LastIndexOf('/') + 1) + this.GetSkinValue(curLifeStageIndex, index, "texName", flag);
                    }
                    this.pawn_renderer.graphics.ClearCache();
                    Graphic_Multi nakedGraphic = (Graphic_Multi)GraphicDatabase.Get<Graphic_Multi>(text3, shader, vector, color, color2);
                    this.pawn_renderer.graphics.nakedGraphic = nakedGraphic;
                    this.skin_index = index;
                    result = true;
                }
            }
            return result;
        }

        public override void TickRare()
        {
            if (base.Map != null)
            {
                if (base.Map.mapTemperature.OutdoorTemp < 0f && this.winter_coat_timer < 50)
                {
                    this.winter_coat_timer++;
                    if (this.winter_coat_timer == 50)
                    {
                        this.winterized = true;
                        this.ApplyNewSkin(this.skin_index);
                    }
                }
                if (base.Map.mapTemperature.OutdoorTemp > 0f && this.winter_coat_timer > 0)
                {
                    this.winter_coat_timer--;
                    Log.Message("winter_coat_timer at " + this.winter_coat_timer);
                    if (this.winter_coat_timer == 0)
                    {
                        this.winterized = false;
                        this.ApplyNewSkin(this.skin_index);
                    }
                }
            }
            base.TickRare();
        }

        public override void Tick()
        {
            base.Tick();
            if (this.ageTracker.CurLifeStageIndex != this.last_age_stage && this.skin_index != -1)
            {
                this.last_age_stage = this.ageTracker.CurLifeStageIndex;
                LongEventHandler.ExecuteWhenFinished(delegate
                {
                    this.ApplyNewSkin(this.skin_index);
                });
            }
        }

        private void CheckRelatives()
        {
            Pawn pawn = null;
            Pawn pawn2 = null;
            if (this.relations.DirectRelations.Count > 0)
            {
                foreach (DirectPawnRelation current in this.relations.DirectRelations)
                {
                    if (current.def == PawnRelationDefOf.Parent)
                    {
                        if (current.otherPawn.gender == Gender.Female)
                        {
                            pawn = current.otherPawn;
                        }
                        else if (current.otherPawn.gender == Gender.Male)
                        {
                            pawn2 = current.otherPawn;
                        }
                    }
                }
            }
            else
            {
                Log.Warning("Fug, no relations!");
            }
            if (pawn != null)
            {
                int index;
                if (pawn2 != null)
                {
                    index = ((AnimalMultiSkins)GenCollection.RandomElement<Pawn>(new List<Pawn>
                    {
                        pawn,
                        pawn2
                    })).skin_index;
                }
                else
                {
                    index = ((AnimalMultiSkins)pawn).skin_index;
                }
                this.ApplyNewSkin(index);
            }
            else
            {
                Log.Message("No mother found");
            }
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            this.male_graphic = this.ageTracker.CurKindLifeStage.bodyGraphicData.texPath;
            bool female = false;
            if (this.ageTracker.CurKindLifeStage.femaleGraphicData != null)
            {
                this.female_graphic = this.ageTracker.CurKindLifeStage.femaleGraphicData.texPath;
                if (this.gender == Gender.Female)
                {
                    female = true;
                }
            }
            this.pawn_renderer = ((Pawn_DrawTracker)typeof(Pawn).GetField("drawer", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(this)).renderer;
            if (!this.TryLoadSkinSet())
            {
                Log.Error("Couldn't find " + this.def.defName + "_SkinSet.xml! This file is required for animals using the variations framework.");
            }
            else
            {
                int curLifeStageIndex = this.ageTracker.CurLifeStageIndex;
                if (this.skin_index == -1)
                {
                    if (base.Map.mapTemperature.OutdoorTemp < 0f)
                    {
                        this.winter_coat_timer = 50;
                    }
                    int new_skin = Rand.Range(0, this.num_skins(curLifeStageIndex, female) + 1);
                    if (AnimalMultiSkins.skinset_xml != null && new_skin != 0)
                    {
                        int num = 100;
                        float num2 = float.Parse(this.GetSkinValue(curLifeStageIndex, new_skin, "commonality", female));
                        while (num2 <= Rand.Value && num > 0)
                        {
                            new_skin = Rand.Range(0, this.num_skins(curLifeStageIndex, female) + 1);
                            if (new_skin == 0)
                            {
                                num2 = 1f;
                            }
                            else
                            {
                                num2 = float.Parse(this.GetSkinValue(curLifeStageIndex, new_skin, "commonality", female));
                            }
                            num--;
                        }
                        if (num == 0)
                        {
                            new_skin = 0;
                        }
                    }
                    int num3 = (int)(float.Parse(this.GetSkinValue(curLifeStageIndex, new_skin, "commonality", female)) / (float)(this.num_skins(curLifeStageIndex, female) + 1) * 100f);
                    Log.Message(string.Concat(new object[]
                    {
                        "Creating new ",
                        this.gender.ToString().ToLower(),
                        " ",
                        this.def.defName,
                        " with skin index of ",
                        new_skin,
                        " where ",
                        this.num_skins(curLifeStageIndex, female),
                        " possible variants were available. Chance was ",
                        num3,
                        "%."
                    }));
                    LongEventHandler.ExecuteWhenFinished(delegate
                    {
                        this.ApplyNewSkin(new_skin);
                    });
                    this.last_age_stage = this.ageTracker.CurLifeStageIndex;
                }
                else
                {
                    LongEventHandler.ExecuteWhenFinished(delegate
                    {
                        this.ApplyNewSkin(this.skin_index);
                    });
                }
            }
        }
    }
}