using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;

public class OrientationChange : MonoBehaviour
{
  [SerializeField] private RectTransform UIWrapper;
  [SerializeField] private CanvasScaler CanvasScaler;
  [SerializeField] private float MatchWidth = 0.25f;
  [SerializeField] private float MatchHeight = 1f;
  [SerializeField] private float PortraitMatchWandH = 0.5f;
  [SerializeField] private float transitionDuration = 0.5f;
  [SerializeField] private float waitForRotation = 1f;

  private Vector2 ReferenceAspect;
  private Tween matchTween;
  private Tween rotationTween;
  private Coroutine rotationRoutine;
  private bool isLandscape;
  private void Awake()
  {
    ReferenceAspect = CanvasScaler.referenceResolution;
  }

  void SwitchDisplay(string dimensions)
  {
    if (rotationRoutine != null) StopCoroutine(rotationRoutine);
    rotationRoutine = StartCoroutine(RotationCoroutine(dimensions));
  }

  IEnumerator RotationCoroutine(string dimensions)
  {
    yield return new WaitForSecondsRealtime(waitForRotation);
    string[] parts = dimensions.Split(',');
    if (parts.Length == 2 && int.TryParse(parts[0], out int width) && int.TryParse(parts[1], out int height) && width > 0 && height > 0)
    {
      Debug.Log($"Unity: Received Dimensions - Width: {width}, Height: {height}");

      isLandscape = width > height;

      Quaternion targetRotation = isLandscape ? Quaternion.identity : Quaternion.Euler(0, 0, -90);
      if (rotationTween != null && rotationTween.IsActive()) rotationTween.Kill();
      rotationTween = UIWrapper.DOLocalRotateQuaternion(targetRotation, transitionDuration).SetEase(Ease.OutCubic);

      float currentAspectRatio = isLandscape ? (float)width / height : (float)height / width;
      float referenceAspectRatio = ReferenceAspect.x / ReferenceAspect.y;

      float targetMatch = isLandscape ? (currentAspectRatio > referenceAspectRatio ? MatchHeight : MatchWidth) : PortraitMatchWandH;
      if (matchTween != null && matchTween.IsActive()) matchTween.Kill();
      matchTween = DOTween.To(() => CanvasScaler.matchWidthOrHeight, x => CanvasScaler.matchWidthOrHeight = x, targetMatch, transitionDuration).SetEase(Ease.InOutQuad);

      Debug.Log($"matchWidthOrHeight set to: {targetMatch}");
    }
    else
    {
      Debug.LogWarning("Unity: Invalid format received in SwitchDisplay");
    }
  }


#if UNITY_EDITOR
  private void Update()
  {
    if (Input.GetKeyDown(KeyCode.K))
    {
      SwitchDisplay(Screen.width + "," + Screen.height);  
    }
  }
#endif
}
