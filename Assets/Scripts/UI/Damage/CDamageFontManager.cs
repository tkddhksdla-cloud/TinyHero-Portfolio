using System.Collections.Generic;
using TinyHero.Core;
using UnityEngine;

namespace TinyHero.UI
{
    ///<summary>
    /// 데미지 폰트 풀링 매니저
    ///</summary>
    public sealed class CDamageFontManager : MonoBehaviour
    {
        private const string DamageFontPrefabResourcePath = "Prefabs/UI/Damage/DamageFontObject";
        private const float DefaultMonsterWorldOffsetY = 0.25f;
        private const float DefaultPlayerWorldOffsetY = 0.45f;
        private const float DefaultDamageFontRandomOffsetX = 0.14f;
        private const float DefaultDamageFontRandomOffsetY = 0.08f;

        [SerializeField] private RectTransform damageFontRootRectTransform;
        [SerializeField] private Canvas targetCanvas;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private CDamageFontObject damageFontPrefab;
        [SerializeField] private Color monsterNormalDamageColor = new Color32( 0xFF, 0xEF, 0x00, 0xFF );
        [SerializeField] private Color monsterCriticalDamageColor = new Color32( 0xFF, 0x41, 0x00, 0xFF );
        [SerializeField] private Color playerDamageColor = new Color32( 0x53, 0x97, 0xFF, 0xFF );
        [SerializeField] private float monsterWorldOffsetY = DefaultMonsterWorldOffsetY;
        [SerializeField] private float playerWorldOffsetY = DefaultPlayerWorldOffsetY;
        [SerializeField] private float damageFontRandomOffsetX = DefaultDamageFontRandomOffsetX;
        [SerializeField] private float damageFontRandomOffsetY = DefaultDamageFontRandomOffsetY;

        private static CDamageFontManager instance;

        private readonly List<CDamageFontObject> activeDamageFontObjectList = new List<CDamageFontObject>();
        private CObjectPool<CDamageFontObject> damageFontPool;

        ///<summary>
        /// 싱글 인스턴스 초기화
        ///</summary>
        private void Awake()
        {
            instance = this;
            ResolveReferences();
            EnsurePoolInitialized();
        }

        ///<summary>
        /// 인스턴스 해제 처리
        ///</summary>
        private void OnDestroy()
        {
            if ( instance == this )
            {
                instance = null;
            }
        }

        ///<summary>
        /// 데미지 폰트 매니저 조회 시도
        ///</summary>
        public static bool TryGetInstance( out CDamageFontManager _instance )
        {
            _instance = instance;
            bool hasInstance = _instance != null;
            return hasInstance;
        }

        ///<summary>
        /// 몬스터 데미지 폰트 표시
        ///</summary>
        public void ShowMonsterDamage( MonsterObject _monsterObject, long _damage, bool _isCritical )
        {
            if ( _monsterObject == null || _damage <= 0 )
            {
                return;
            }

            Vector3 worldPosition = _monsterObject.GetMonsterInfoWorldPosition();
            worldPosition.y += monsterWorldOffsetY;
            worldPosition = ApplyRandomWorldOffset( worldPosition );
            Color damageColor = _isCritical ? monsterCriticalDamageColor : monsterNormalDamageColor;
            string damageText = _damage.ToString();
            ShowDamageFont( worldPosition, damageText, damageColor );
        }

        ///<summary>
        /// 플레이어 데미지 폰트 표시
        ///</summary>
        public void ShowPlayerDamage( Transform _targetTransform, float _damage )
        {
            if ( _targetTransform == null || _damage <= 0.0f )
            {
                return;
            }

            Vector3 worldPosition = ResolvePlayerDamageWorldPosition( _targetTransform );
            worldPosition.y += playerWorldOffsetY;
            worldPosition = ApplyRandomWorldOffset( worldPosition );
            int roundedDamage = Mathf.RoundToInt( _damage );
            string damageText = roundedDamage.ToString();
            ShowDamageFont( worldPosition, damageText, playerDamageColor );
        }

        ///<summary>
        /// 데미지 폰트 랜덤 오프셋 적용
        ///</summary>
        private Vector3 ApplyRandomWorldOffset( Vector3 _worldPosition )
        {
            float randomOffsetX = Random.Range( -Mathf.Abs( damageFontRandomOffsetX ), Mathf.Abs( damageFontRandomOffsetX ) );
            float randomOffsetY = Random.Range( -Mathf.Abs( damageFontRandomOffsetY ), Mathf.Abs( damageFontRandomOffsetY ) );
            Vector3 result = _worldPosition + new Vector3( randomOffsetX, randomOffsetY, 0.0f );
            return result;
        }

        ///<summary>
        /// 활성 데미지 폰트 일괄 반환
        ///</summary>
        public void ReleaseAllActiveDamageFonts()
        {
            if ( damageFontPool == null )
            {
                return;
            }

            List<CDamageFontObject> copiedActiveDamageFontObjectList = new List<CDamageFontObject>( activeDamageFontObjectList );
            int activeDamageFontCount = copiedActiveDamageFontObjectList.Count;

            for ( int index = 0; index < activeDamageFontCount; index++ )
            {
                CDamageFontObject damageFontObject = copiedActiveDamageFontObjectList[ index ];

                if ( damageFontObject == null )
                {
                    continue;
                }

                damageFontPool.Release( damageFontObject );
            }
        }

        ///<summary>
        /// 데미지 폰트 화면 표시 처리
        ///</summary>
        private void ShowDamageFont( Vector3 _worldPosition, string _damageText, Color _damageColor )
        {
            ResolveReferences();
            EnsurePoolInitialized();

            if ( damageFontPool == null || damageFontRootRectTransform == null )
            {
                return;
            }

            Camera resolvedWorldCamera = ResolveWorldCamera();
            Vector3 screenPosition = resolvedWorldCamera != null
                ? resolvedWorldCamera.WorldToScreenPoint( _worldPosition )
                : RectTransformUtility.WorldToScreenPoint( null, _worldPosition );

            if ( screenPosition.z < 0.0f )
            {
                return;
            }

            Camera canvasCamera = targetCanvas != null && targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? targetCanvas.worldCamera
                : null;
            bool wasResolved = RectTransformUtility.ScreenPointToLocalPointInRectangle( damageFontRootRectTransform, screenPosition, canvasCamera, out Vector2 localPoint );

            if ( wasResolved == false )
            {
                return;
            }

            CDamageFontObject damageFontObject = damageFontPool.Get();

            if ( damageFontObject == null )
            {
                return;
            }

            damageFontObject.SetDisplay( _damageText, _damageColor, localPoint );

            if ( activeDamageFontObjectList.Contains( damageFontObject ) == false )
            {
                activeDamageFontObjectList.Add( damageFontObject );
            }
        }

        ///<summary>
        /// 플레이어 데미지 기준 위치 계산
        ///</summary>
        private Vector3 ResolvePlayerDamageWorldPosition( Transform _targetTransform )
        {
            Collider2D targetCollider = _targetTransform.GetComponent<Collider2D>();

            if ( targetCollider == null )
            {
                Collider2D resolvedChildCollider = _targetTransform.GetComponentInChildren<Collider2D>();
                targetCollider = resolvedChildCollider;
            }

            if ( targetCollider == null )
            {
                Vector3 fallbackPosition = _targetTransform.position;
                return fallbackPosition;
            }

            Bounds colliderBounds = targetCollider.bounds;
            Vector3 worldPosition = new Vector3( colliderBounds.center.x, colliderBounds.max.y, colliderBounds.center.z );
            return worldPosition;
        }

        ///<summary>
        /// 참조 컴포넌트 자동 연결
        ///</summary>
        private void ResolveReferences()
        {
            if ( damageFontRootRectTransform == null )
            {
                RectTransform resolvedRectTransform = transform as RectTransform;
                damageFontRootRectTransform = resolvedRectTransform;
            }

            if ( targetCanvas == null )
            {
                Canvas resolvedCanvas = GetComponentInParent<Canvas>();
                targetCanvas = resolvedCanvas;
            }

            if ( worldCamera == null )
            {
                Camera resolvedWorldCamera = Camera.main;
                worldCamera = resolvedWorldCamera;
            }

            if ( damageFontPrefab != null )
            {
                return;
            }

            CDamageFontObject loadedPrefab = Resources.Load<CDamageFontObject>( DamageFontPrefabResourcePath );
            damageFontPrefab = loadedPrefab;
        }

        ///<summary>
        /// 데미지 폰트 풀 초기화 보장
        ///</summary>
        private void EnsurePoolInitialized()
        {
            if ( damageFontPool != null )
            {
                return;
            }

            if ( damageFontPrefab == null || damageFontRootRectTransform == null )
            {
                return;
            }

            CObjectPool<CDamageFontObject> createdPool = new CObjectPool<CDamageFontObject>(
                CreateDamageFontObject,
                OnGetDamageFontObject,
                OnReleaseDamageFontObject );
            damageFontPool = createdPool;
        }

        ///<summary>
        /// 데미지 폰트 인스턴스 생성
        ///</summary>
        private CDamageFontObject CreateDamageFontObject()
        {
            if ( damageFontPrefab == null || damageFontRootRectTransform == null )
            {
                return null;
            }

            CDamageFontObject createdDamageFontObject = Instantiate( damageFontPrefab, damageFontRootRectTransform );
            createdDamageFontObject.name = damageFontPrefab.name;
            createdDamageFontObject.SetReturnToPoolHandler( HandleAutoReturnDamageFontObject );
            createdDamageFontObject.gameObject.SetActive( false );
            return createdDamageFontObject;
        }

        ///<summary>
        /// 데미지 폰트 대여 후처리
        ///</summary>
        private void OnGetDamageFontObject( CDamageFontObject _damageFontObject )
        {
            if ( _damageFontObject == null )
            {
                return;
            }

            _damageFontObject.transform.SetParent( damageFontRootRectTransform, false );
            _damageFontObject.gameObject.SetActive( true );
        }

        ///<summary>
        /// 데미지 폰트 반환 후처리
        ///</summary>
        private void OnReleaseDamageFontObject( CDamageFontObject _damageFontObject )
        {
            if ( _damageFontObject == null )
            {
                return;
            }

            activeDamageFontObjectList.Remove( _damageFontObject );
            _damageFontObject.transform.SetParent( damageFontRootRectTransform, false );
            _damageFontObject.gameObject.SetActive( false );
        }

        ///<summary>
        /// 데미지 폰트 자동 반환 처리
        ///</summary>
        private void HandleAutoReturnDamageFontObject( CAutoPoolReturnObject _autoPoolReturnObject )
        {
            if ( damageFontPool == null )
            {
                return;
            }

            CDamageFontObject damageFontObject = _autoPoolReturnObject as CDamageFontObject;

            if ( damageFontObject == null )
            {
                return;
            }

            damageFontPool.Release( damageFontObject );
        }

        ///<summary>
        /// 월드 카메라 참조 결정
        ///</summary>
        private Camera ResolveWorldCamera()
        {
            if ( worldCamera != null )
            {
                return worldCamera;
            }

            if ( Camera.main != null )
            {
                worldCamera = Camera.main;
            }

            Camera result = worldCamera;
            return result;
        }
    }
}
