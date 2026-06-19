using UnityEditor;
using UnityEngine;

namespace TinyHero.Skill.Editor
{
    ///<summary>
    /// 스킬 에디터 VFX 정렬 레이어 자동 보정 유틸리티
    ///</summary>
    public static class CSkillEditorVfxSortingUtility
    {
        private const string SkillEffectSortingLayerName = "SkillEffect";

        ///<summary>
        /// 스킬 VFX 프리팹 정렬 레이어 자동 보정
        ///</summary>
        public static void ApplySkillEffectSortingLayer( GameObject _prefab )
        {
            if ( _prefab == null )
            {
                return;
            }

            if ( SortingLayerExists() == false )
            {
                return;
            }

            string prefabAssetPath = AssetDatabase.GetAssetPath( _prefab );

            if ( string.IsNullOrWhiteSpace( prefabAssetPath ) )
            {
                return;
            }

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents( prefabAssetPath );

            if ( prefabRoot == null )
            {
                return;
            }

            bool isChanged = false;
            ParticleSystemRenderer[] particleRendererArray = prefabRoot.GetComponentsInChildren<ParticleSystemRenderer>( true );
            int particleRendererCount = particleRendererArray.Length;

            for ( int index = 0; index < particleRendererCount; index++ )
            {
                ParticleSystemRenderer particleRenderer = particleRendererArray[ index ];

                if ( particleRenderer == null )
                {
                    continue;
                }

                if ( string.Equals( particleRenderer.sortingLayerName, SkillEffectSortingLayerName, System.StringComparison.Ordinal ) )
                {
                    continue;
                }

                particleRenderer.sortingLayerName = SkillEffectSortingLayerName;
                EditorUtility.SetDirty( particleRenderer );
                isChanged = true;
            }

            if ( isChanged )
            {
                PrefabUtility.SaveAsPrefabAsset( prefabRoot, prefabAssetPath );
                AssetDatabase.SaveAssets();
            }

            PrefabUtility.UnloadPrefabContents( prefabRoot );
        }

        ///<summary>
        /// 스킬 이펙트 정렬 레이어 존재 여부
        ///</summary>
        private static bool SortingLayerExists()
        {
            SortingLayer[] sortingLayerArray = SortingLayer.layers;
            int sortingLayerCount = sortingLayerArray.Length;

            for ( int index = 0; index < sortingLayerCount; index++ )
            {
                SortingLayer sortingLayer = sortingLayerArray[ index ];

                if ( string.Equals( sortingLayer.name, SkillEffectSortingLayerName, System.StringComparison.Ordinal ) == false )
                {
                    continue;
                }

                return true;
            }

            return false;
        }
    }
}
