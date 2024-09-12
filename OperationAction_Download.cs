using Holowerkz_v1_App_ShareCrafter;
using Holowerkz_v1_App_ShareCrafter_TPI;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace Holowerkz_v1_Lib 
{
    public class OperationAction_Download : Operations_APICommon
    {
        private void Awake()
        {
            
        }

        #region RSS Cloud
        public OperationState FetchRSSCloud(string url, RSSCompletedHandler onFinished = null, BaseAbortedHandler onAborted = null, BaseUpdatedHandler onUpdated = null)
        {
            OperationState opState = OperationHandle.OpMgr.CreateOperation();
            void HandleFetchCompleted(FeedBlock response)
            {
                opState.SetCoroutine(ParseRSSCloud(opState, response));
                OperationHandle.OpMgr.StartOperation(opState);
                onFinished?.Invoke(response);
            }

            OperationState midState = OperationHandle.OpMgr.CreateOperation();
            midState.SetCoroutine(GetOneFeedCoroutine(url, midState, 
                HandleFetchCompleted, 
                onAborted ?? GenericDownloadRequestAborted, 
                onUpdated ?? GenericHandleDownloadRequestUpdated));
            OperationHandle.OpMgr.StartOperation(midState);

            return opState;
        }

        private IEnumerator GetOneFeedCoroutine(string url, OperationState opState, RSSCompletedHandler onFinished, BaseAbortedHandler onAborted, BaseUpdatedHandler onUpdated)
        {
            if(IsValidURL(url) == false) {
                //MainRoot.AppBar.OpenPopup();
                onAborted?.Invoke("GetOneFeedCoroutine Invalid URL");
                Debug.Log($"GetOneFeedCoroutine failed {url}");
                yield break;
            }

            using(var request = UnityWebRequest.Get(url)) {

                SetRequestHeader(request, false, false);

                yield return SendWebRequestCoroutine(request, onUpdated);

                if(ValidateResponse(request, out FeedBlock response, onAborted)) {
                    opState?.Complete();
                    onFinished?.Invoke(response);
                } else {
                    opState.Abort();
                    onAborted?.Invoke("GetOneFeedCoroutine Aborted");
                    Debug.Log("Operations_APIData GetOneFeedCoroutine Abort");
                }
            }
        }

        private IEnumerator ParseRSSCloud(OperationState opState, FeedBlock response, BaseCompletedHandler onFinished = null, BaseAbortedHandler onAborted = null, BaseUpdatedHandler onUpdated = null)
        {
            //foreach(string str in response) {
            //    Debug.Log($"ParseRSSCloud {str}");
            //}

            Debug.Log("ParseRSSCloud Complete");
            opState.Complete();

            yield return new WaitForEndOfFrame();
        }
        #endregion

        #region Lazy Thumbs
        public OperationState RequestLazyThumb(string thumbURL, BaseImageDownloadHandler onDownloaded, BaseAbortedHandler onAborted = null)
        {
            OperationState midState = OperationHandle.OpMgr.CreateOperation();
            void HandleFetchCompleted(Texture texture) 
            {
                midState.Complete();
                onDownloaded?.Invoke(texture);
		    }

            midState.SetCoroutine(RequestThumb(midState, thumbURL, 
            HandleFetchCompleted,
            onAborted ?? GenericDownloadRequestAborted));
            OperationHandle.OpMgr.StartOperation(midState);

            return midState;
        }

        private IEnumerator RequestThumb(OperationState opState, string thumbURL, BaseImageDownloadHandler onDownloaded, BaseAbortedHandler onAborted = null)
        {
            Texture thumbnail = MainRoot.DataThumbs.FindThumbnail(thumbURL);
            if(thumbnail == null && !string.IsNullOrEmpty(thumbURL)) {
                yield return DownloadImageCoroutine(thumbURL,
                    texture2D => {
                        MainRoot.DataThumbs.CacheThumbnail(thumbURL, texture2D);
                        opState.Complete();
                        onDownloaded.Invoke(texture2D);
                    },
                    errorMessage => {
                        opState.Abort();
                        onAborted?.Invoke(errorMessage);
                    });
            } else if(thumbnail != null) {
                opState.Complete();
                onDownloaded?.Invoke(thumbnail);
            }
        }

        private IEnumerator DownloadImageCoroutine(string url, BaseImageDownloadHandler onDownloaded, BaseAbortedHandler onAborted, BaseUpdatedHandler onUpdated = null)
        {
            if (string.IsNullOrEmpty(url)) {
                onAborted?.Invoke("URL is null or empty.");
                yield break;
            }

            using (var request = UnityWebRequestTexture.GetTexture(url)) {

                yield return SendWebRequestCoroutine(request, onUpdated);

                if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError) {
                    onAborted?.Invoke(OperationHandle.FormatMessage(request));
                } else {
                    onDownloaded?.Invoke(((DownloadHandlerTexture)request.downloadHandler).texture);
                }
            }
        }
        #endregion

        #region Generic Handlers
        private void GenericDownloadRequestAborted(string message)
        {
            Debug.Log($"OperationAction_Download GenericDownloadRequestAborted {message}");
        }
        #endregion
    }
}
