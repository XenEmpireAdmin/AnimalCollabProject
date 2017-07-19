﻿using System;
using UnityEngine;
using Verse;
using RimWorld;

namespace SpiritWolfEvent
{

        public class IncidentWorker_SpiritWolves : IncidentWorker
        {
            protected override bool CanFireNowSub(IIncidentTarget target)
            {
                Map map = (Map)target;
                return !map.gameConditionManager.ConditionIsActive(GameConditionDefOf.ToxicFallout);
            }

            public override bool TryExecute(IncidentParms parms)
            {
                Map map = (Map)parms.target;
                IntVec3 intVec;
                if (!RCellFinder.TryFindRandomPawnEntryCell(out intVec, map, CellFinder.EdgeRoadChance_Animal + 0.2f, null))
                {
                    return false;
                }
                PawnKindDef spiritwolf = PawnKindDefOf.ACPSpiritwolf;
                float points = StorytellerUtility.DefaultParmsNow(Find.Storyteller.def, IncidentCategory.ThreatBig, map).points;
                int num = GenMath.RoundRandom(points / spiritwolf.combatPower);
                int max = Rand.RangeInclusive(2, 4);
                num = Mathf.Clamp(num, 1, max);
                int num2 = Rand.RangeInclusive(90000, 150000);
                IntVec3 invalid = IntVec3.Invalid;
                if (!RCellFinder.TryFindRandomCellOutsideColonyNearTheCenterOfTheMap(intVec, map, 10f, out invalid))
                {
                    invalid = IntVec3.Invalid;
                }
                Pawn pawn = null;
                for (int i = 0; i < num; i++)
                {
                    IntVec3 loc = CellFinder.RandomClosewalkCellNear(intVec, map, 10, null);
                    pawn = PawnGenerator.GeneratePawn(spiritwolf, null);
                    GenSpawn.Spawn(pawn, loc, map, Rot4.Random, false);
                    pawn.mindState.exitMapAfterTick = Find.TickManager.TicksGame + num2;
                    if (invalid.IsValid)
                    {
                        pawn.mindState.forcedGotoPosition = CellFinder.RandomClosewalkCellNear(invalid, map, 10, null);
                    }
                }
                Find.LetterStack.ReceiveLetter("LetterLabelSpiritWolfHunt".Translate(new object[]
                {
                spiritwolf.label
                }).CapitalizeFirst(), "LetterSpiritWolfHunt".Translate(new object[]
                {
                spiritwolf.label
                }), LetterDefOf.Good, pawn, null);
                return true;
            }
        }
    }