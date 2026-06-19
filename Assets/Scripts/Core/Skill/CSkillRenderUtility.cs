using UnityEngine;
using UnityEngine.Rendering;

namespace TinyHero.Skill
{
    ///<summary>
    /// 스킬 이펙트 전면 정렬 보조 유틸리티
    ///</summary>
    public static class CSkillRenderUtility
    {
        private const int SkillEffectSortingOrderOffset = 200;

        ///<summary>
        /// 스킬 이펙트 렌더러 전면 정렬 적용
        ///</summary>
        public static void ApplyForegroundSorting( GameObject _targetObject )
        {
            if ( _targetObject == null )
            {
                return;
            }

            ApplySortingGroupOffset( _targetObject );
            ApplyRendererOffset( _targetObject );
        }

        ///<summary>
        /// 스킬 이펙트 정렬 그룹 오프셋 적용
        ///</summary>
        private static void ApplySortingGroupOffset( GameObject _targetObject )
        {
            SortingGroup[] sortingGroupArray = _targetObject.GetComponentsInChildren<SortingGroup>( true );
            int sortingGroupCount = sortingGroupArray.Length;

            for ( int index = 0; index < sortingGroupCount; index++ )
            {
                SortingGroup sortingGroup = sortingGroupArray[ index ];

                if ( sortingGroup == null )
                {
                    continue;
                }

                sortingGroup.sortingOrder += SkillEffectSortingOrderOffset;
            }
        }

        ///<summary>
        /// 스킬 이펙트 렌더러 오프셋 적용
        ///</summary>
        private static void ApplyRendererOffset( GameObject _targetObject )
        {
            Renderer[] rendererArray = _targetObject.GetComponentsInChildren<Renderer>( true );
            int rendererCount = rendererArray.Length;

            for ( int index = 0; index < rendererCount; index++ )
            {
                Renderer targetRenderer = rendererArray[ index ];

                if ( targetRenderer == null )
                {
                    continue;
                }

                targetRenderer.sortingOrder += SkillEffectSortingOrderOffset;
            }
        }
    }
}
