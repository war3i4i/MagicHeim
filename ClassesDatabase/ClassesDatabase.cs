﻿using MagicHeim.ClassesDatabase.Classes;
using MagicHeim.MH_Enums;
using MagicHeim.MH_Interfaces;

namespace MagicHeim.ClassesDatabase;

public static class ClassesDatabase
{
    private static readonly Dictionary<Class, MH_ClassDefinition> GLOBAL_ClassesDefinitions = new();

    public static MH_ClassDefinition GetClassDefinition(Class @class)
    {
        return GLOBAL_ClassesDefinitions.TryGetValue(@class, out MH_ClassDefinition definition) ? definition : null;
    }

    private static void AddClass(Class @class, MH_ClassDefinition classDefinition)
    {
        GLOBAL_ClassesDefinitions[@class] = classDefinition;
    }


    public static void Init()
    {
        AddClass(Class.Warrior, new Warrior("Warrior", "Warrior Description"));
        AddClass(Class.Mage, new Mage("Mage", "$mh_mageclass_description"));
        AddClass(Class.Druid, new Druid("Druid", "$mh_druidclass_description"));
        foreach (KeyValuePair<Class, MH_ClassDefinition> classesDefinition in GLOBAL_ClassesDefinitions)
            classesDefinition.Value.Init();
    }
    

    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    static class ZNetScene_Awake_Patch
    {
        static void Postfix(ZNetScene __instance)
        {
            foreach (KeyValuePair<Class, MH_ClassDefinition> classesDefinition in GLOBAL_ClassesDefinitions)
            {
                __instance.m_namedPrefabs[classesDefinition.Value.OnSelect_VFX.name.GetStableHashCode()] =
                    classesDefinition.Value.OnSelect_VFX;
            }
        }
    }
}