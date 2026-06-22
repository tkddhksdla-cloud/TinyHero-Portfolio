using System.Collections;
using System.Collections.Generic;
using LayerLab.ArtMakerUnity;
using TinyHero.Core.Data;
using UnityEngine;

namespace TinyHero.Player
{
    ///<summary>
    /// 플레이어 장비와 파츠 외형 동기화
    ///</summary>
    [RequireComponent( typeof( CPlayerEquipmentManager ) )]
    public sealed class CPlayerEquipmentPartsSync : MonoBehaviour
    {
        private static readonly PartsType[] ManagedPartsTypeArray =
        {
            PartsType.Sword,
            PartsType.Axe,
            PartsType.Bow,
            PartsType.Wand,
            PartsType.Staff,
            PartsType.Spear,
            PartsType.Blunt,
            PartsType.Crossbow,
            PartsType.Shield,
            PartsType.SubItem,
            PartsType.Helmet,
            PartsType.Chest
        };

        [SerializeField] private CPlayerEquipmentManager targetEquipmentManager;
        [SerializeField] private PartsManager targetPartsManager;

        private readonly Dictionary<PartsType, int> defaultPartsIndexDictionary = new Dictionary<PartsType, int>();
        private readonly Dictionary<PartsType, bool> defaultPartsVisibilityDictionary = new Dictionary<PartsType, bool>();

        private bool isDefaultStateCached;

        ///<summary>
        /// 장비 매니저와 파츠 매니저 참조 결정
        ///</summary>
        private void Awake()
        {
            ResolveReferences();
        }

        ///<summary>
        /// 장비 변경 이벤트 구독
        ///</summary>
        private void OnEnable()
        {
            ResolveReferences();
            SubscribeEquipmentChanged();
        }

        ///<summary>
        /// 초기 파츠 상태 캐시 시작
        ///</summary>
        private void Start()
        {
            StartCoroutine( IE_InitializePartsState() );
        }

        ///<summary>
        /// 장비 변경 이벤트 구독 해제
        ///</summary>
        private void OnDisable()
        {
            UnsubscribeEquipmentChanged();
        }

        ///<summary>
        /// 장비 변경 외형 반영
        ///</summary>
        private void HandleEquipmentChanged( CPlayerEquipmentManager _equipmentManager )
        {
            if ( isDefaultStateCached == false )
            {
                return;
            }

            ApplyEquipmentPartsState();
        }

        ///<summary>
        /// 초기 파츠 상태 지연 캐시
        ///</summary>
        private IEnumerator IE_InitializePartsState()
        {
            yield return null;
            ResolveReferences();
            CacheDefaultPartsState();
            ApplyEquipmentPartsState();
        }

        ///<summary>
        /// 장비 매니저와 파츠 매니저 참조 결정
        ///</summary>
        private void ResolveReferences()
        {
            if ( targetEquipmentManager == null )
            {
                bool hasEquipmentManager = TryGetComponent( out CPlayerEquipmentManager resolvedEquipmentManager );

                if ( hasEquipmentManager )
                {
                    targetEquipmentManager = resolvedEquipmentManager;
                }
            }

            if ( targetPartsManager == null )
            {
                PartsManager resolvedPartsManager = GetComponentInChildren<PartsManager>( true );

                if ( resolvedPartsManager != null )
                {
                    targetPartsManager = resolvedPartsManager;
                }
            }
        }

        ///<summary>
        /// 장비 변경 이벤트 구독
        ///</summary>
        private void SubscribeEquipmentChanged()
        {
            if ( targetEquipmentManager == null )
            {
                return;
            }

            targetEquipmentManager.OnEquipmentChanged -= HandleEquipmentChanged;
            targetEquipmentManager.OnEquipmentChanged += HandleEquipmentChanged;
        }

        ///<summary>
        /// 장비 변경 이벤트 구독 해제
        ///</summary>
        private void UnsubscribeEquipmentChanged()
        {
            if ( targetEquipmentManager == null )
            {
                return;
            }

            targetEquipmentManager.OnEquipmentChanged -= HandleEquipmentChanged;
        }

        ///<summary>
        /// 기본 파츠 상태 캐시
        ///</summary>
        private void CacheDefaultPartsState()
        {
            if ( isDefaultStateCached || targetPartsManager == null )
            {
                return;
            }

            defaultPartsIndexDictionary.Clear();
            defaultPartsVisibilityDictionary.Clear();

            for ( int index = 0; index < ManagedPartsTypeArray.Length; index++ )
            {
                PartsType partsType = ManagedPartsTypeArray[ index ];
                int partsIndex = targetPartsManager.GetActiveIndex( partsType );
                bool isVisible = targetPartsManager.IsPartsVisible( partsType );
                defaultPartsIndexDictionary[ partsType ] = partsIndex;
                defaultPartsVisibilityDictionary[ partsType ] = isVisible;
            }

            isDefaultStateCached = true;
        }

        ///<summary>
        /// 장비 외형 전체 반영
        ///</summary>
        private void ApplyEquipmentPartsState()
        {
            if ( targetEquipmentManager == null || targetPartsManager == null )
            {
                return;
            }

            ApplyEquipmentPartsState( eEquipmentType.WEAPON );
            ApplyEquipmentPartsState( eEquipmentType.HELMET );
            ApplyEquipmentPartsState( eEquipmentType.ARMOR );
            ApplyEquipmentPartsState( eEquipmentType.SHIELD );
        }

        ///<summary>
        /// 장비 슬롯 외형 반영
        ///</summary>
        private void ApplyEquipmentPartsState( eEquipmentType _equipmentType )
        {
            CItemDefinition equippedItemDefinition = targetEquipmentManager.GetEquippedItemDefinition( _equipmentType );

            if ( equippedItemDefinition == null )
            {
                RestoreDefaultPartsState( _equipmentType );
                return;
            }

            HideEquipmentPartsState( _equipmentType );

            if ( equippedItemDefinition.HasEquipmentPartsVisual() == false )
            {
                return;
            }

            PartsType equipmentPartsType = equippedItemDefinition.GetEquipmentPartsType();

            if ( IsCompatiblePartsType( _equipmentType, equipmentPartsType ) == false )
            {
                return;
            }

            int equipmentPartsIndex = equippedItemDefinition.GetEquipmentPartsIndex();

            if ( equipmentPartsIndex < 0 )
            {
                return;
            }

            targetPartsManager.EquipParts( equipmentPartsType, equipmentPartsIndex );
        }

        ///<summary>
        /// 장비 슬롯 기본 파츠 복원
        ///</summary>
        private void RestoreDefaultPartsState( eEquipmentType _equipmentType )
        {
            PartsType[] partsTypeArray = ResolveManagedPartsTypeArray( _equipmentType );

            for ( int index = 0; index < partsTypeArray.Length; index++ )
            {
                PartsType partsType = partsTypeArray[ index ];
                bool hasDefaultIndex = defaultPartsIndexDictionary.TryGetValue( partsType, out int defaultPartsIndex );
                bool hasDefaultVisibility = defaultPartsVisibilityDictionary.TryGetValue( partsType, out bool defaultVisibility );

                if ( hasDefaultIndex == false || hasDefaultVisibility == false || defaultVisibility == false || defaultPartsIndex < 0 )
                {
                    targetPartsManager.UnequipParts( partsType );
                    continue;
                }

                targetPartsManager.EquipParts( partsType, defaultPartsIndex );
            }
        }

        ///<summary>
        /// 장비 슬롯 파츠 숨김 처리
        ///</summary>
        private void HideEquipmentPartsState( eEquipmentType _equipmentType )
        {
            PartsType[] partsTypeArray = ResolveManagedPartsTypeArray( _equipmentType );

            for ( int index = 0; index < partsTypeArray.Length; index++ )
            {
                PartsType partsType = partsTypeArray[ index ];
                targetPartsManager.UnequipParts( partsType );
            }
        }

        ///<summary>
        /// 장비 슬롯 관리 파츠 배열 반환
        ///</summary>
        private PartsType[] ResolveManagedPartsTypeArray( eEquipmentType _equipmentType )
        {
            switch ( _equipmentType )
            {
                case eEquipmentType.WEAPON:
                    return new PartsType[]
                    {
                        PartsType.Sword,
                        PartsType.Axe,
                        PartsType.Bow,
                        PartsType.Wand,
                        PartsType.Staff,
                        PartsType.Spear,
                        PartsType.Blunt,
                        PartsType.Crossbow
                    };

                case eEquipmentType.HELMET:
                    return new PartsType[]
                    {
                        PartsType.Helmet
                    };

                case eEquipmentType.ARMOR:
                    return new PartsType[]
                    {
                        PartsType.Chest
                    };

                case eEquipmentType.SHIELD:
                    return new PartsType[]
                    {
                        PartsType.Shield,
                        PartsType.SubItem
                    };
            }

            return new PartsType[ 0 ];
        }

        ///<summary>
        /// 장비 슬롯과 파츠 타입 호환 여부 판단
        ///</summary>
        private bool IsCompatiblePartsType( eEquipmentType _equipmentType, PartsType _partsType )
        {
            PartsType[] compatiblePartsTypeArray = ResolveManagedPartsTypeArray( _equipmentType );

            for ( int index = 0; index < compatiblePartsTypeArray.Length; index++ )
            {
                PartsType compatiblePartsType = compatiblePartsTypeArray[ index ];

                if ( compatiblePartsType != _partsType )
                {
                    continue;
                }

                return true;
            }

            return false;
        }
    }
}
