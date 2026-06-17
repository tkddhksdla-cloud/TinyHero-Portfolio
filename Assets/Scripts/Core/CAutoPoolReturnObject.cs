using System;
using System.Collections;
using UnityEngine;

///<summary>
/// 지연 후 오브젝트풀 자동 반환 컴포넌트
///</summary>
public class CAutoPoolReturnObject : MonoBehaviour
{
    [SerializeField] private float returnDelay = 3.0f;

    private Action<CAutoPoolReturnObject> returnToPoolHandler;
    private Coroutine autoReturnRoutine;

    ///<summary>
    /// 활성화 시 자동 반환 예약
    ///</summary>
    private void OnEnable()
    {
        StartAutoReturnRoutine();
        OnAutoReturnObjectEnabled();
    }

    ///<summary>
    /// 비활성화 시 자동 반환 예약 해제
    ///</summary>
    private void OnDisable()
    {
        StopAutoReturnRoutine();
        OnAutoReturnObjectDisabled();
    }

    ///<summary>
    /// 풀 반환 핸들러 설정
    ///</summary>
    public void SetReturnToPoolHandler( Action<CAutoPoolReturnObject> _returnToPoolHandler )
    {
        returnToPoolHandler = _returnToPoolHandler;
    }

    ///<summary>
    /// 자동 반환 지연 시간 설정
    ///</summary>
    public void SetReturnDelay( float _returnDelay )
    {
        returnDelay = Mathf.Max( 0.0f, _returnDelay );

        if ( isActiveAndEnabled == false )
        {
            return;
        }

        StartAutoReturnRoutine();
    }

    ///<summary>
    /// 즉시 풀 반환 처리
    ///</summary>
    public void ForceReturnToPool()
    {
        StopAutoReturnRoutine();

        if ( returnToPoolHandler != null )
        {
            returnToPoolHandler( this );
            return;
        }

        gameObject.SetActive( false );
    }

    ///<summary>
    /// 활성화 후 추가 처리 지점
    ///</summary>
    protected virtual void OnAutoReturnObjectEnabled()
    {
    }

    ///<summary>
    /// 비활성화 후 추가 처리 지점
    ///</summary>
    protected virtual void OnAutoReturnObjectDisabled()
    {
    }

    ///<summary>
    /// 자동 반환 코루틴 시작
    ///</summary>
    private void StartAutoReturnRoutine()
    {
        StopAutoReturnRoutine();
        autoReturnRoutine = StartCoroutine( IE_AutoReturnToPool() );
    }

    ///<summary>
    /// 자동 반환 코루틴 중단
    ///</summary>
    private void StopAutoReturnRoutine()
    {
        if ( autoReturnRoutine == null )
        {
            return;
        }

        StopCoroutine( autoReturnRoutine );
        autoReturnRoutine = null;
    }

    ///<summary>
    /// 자동 반환 대기 코루틴
    ///</summary>
    private IEnumerator IE_AutoReturnToPool()
    {
        if ( returnDelay > 0.0f )
        {
            yield return new WaitForSeconds( returnDelay );
        }

        autoReturnRoutine = null;
        ForceReturnToPool();
    }
}
