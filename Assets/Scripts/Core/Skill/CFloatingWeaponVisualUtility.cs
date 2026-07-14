using LayerLab.ArtMakerUnity;
using TinyHero.Core.Data;
using TinyHero.Player;
using UnityEngine;

namespace TinyHero.Skill
{
    ///<summary>
    /// 장착 무기 파츠에서 소환 무기 스프라이트를 결정하는 보조 유틸리티
    ///</summary>
    public static class CFloatingWeaponVisualUtility
    {
        private static readonly PartsType[] WeaponPartsTypeArray =
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

        ///<summary>
        /// 현재 장착 무기 또는 기본 표시 무기의 대표 스프라이트 결정
        ///</summary>
        public static bool TryResolveWeaponSprite( CSkillContext _skillContext, out Sprite _weaponSprite )
        {
            _weaponSprite = null;

            if ( _skillContext == null )
            {
                return false;
            }

            PlayerController playerController = _skillContext.GetPlayerController();

            if ( playerController == null )
            {
                return false;
            }

            PartsManager partsManager = playerController.GetComponentInChildren<PartsManager>( true );
            CPlayerEquipmentManager equipmentManager = playerController.GetEquipmentManager();
            CItemDefinition weaponDefinition = equipmentManager != null ? equipmentManager.GetEquippedItemDefinition( eEquipmentType.WEAPON ) : null;

            if ( weaponDefinition != null )
            {
                _weaponSprite = ResolveEquippedWeaponSprite( weaponDefinition, partsManager );
            }

            if ( _weaponSprite == null )
            {
                _weaponSprite = ResolveVisibleWeaponSprite( partsManager );
            }

            bool result = _weaponSprite != null;
            return result;
        }

        private static Sprite ResolveEquippedWeaponSprite( CItemDefinition _weaponDefinition, PartsManager _partsManager )
        {
            if ( _weaponDefinition == null )
            {
                return null;
            }

            Sprite weaponSprite = null;

            if ( _weaponDefinition.HasEquipmentPartsVisual() )
            {
                PartsType weaponPartsType = _weaponDefinition.GetEquipmentPartsType();
                int weaponPartsIndex = _weaponDefinition.GetEquipmentPartsIndex();

                if ( _partsManager != null && weaponPartsIndex >= 0 )
                {
                    weaponSprite = _partsManager.GetThumbnail( weaponPartsType, weaponPartsIndex );
                }
            }

            if ( weaponSprite == null )
            {
                weaponSprite = _weaponDefinition.GetIconSprite();
            }

            Sprite result = weaponSprite;
            return result;
        }

        private static Sprite ResolveVisibleWeaponSprite( PartsManager _partsManager )
        {
            if ( _partsManager == null )
            {
                return null;
            }

            for ( int index = 0; index < WeaponPartsTypeArray.Length; index++ )
            {
                PartsType weaponPartsType = WeaponPartsTypeArray[ index ];

                if ( _partsManager.IsPartsVisible( weaponPartsType ) == false )
                {
                    continue;
                }

                int weaponPartsIndex = _partsManager.GetActiveIndex( weaponPartsType );

                if ( weaponPartsIndex < 0 )
                {
                    continue;
                }

                Sprite weaponSprite = _partsManager.GetThumbnail( weaponPartsType, weaponPartsIndex );

                if ( weaponSprite != null )
                {
                    return weaponSprite;
                }
            }

            return null;
        }
    }
}
