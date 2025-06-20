using UnityEngine;

namespace Lizi.FrameWork.Util
{
    public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
    {
        private static T mInstance = null;
    
        public static T Instance
        {
            get
            {
                if (mInstance == null)
                {
                    mInstance = GameObject.FindObjectOfType(typeof(T)) as T;
                    if (mInstance == null)
                    {
                        GameObject go = new GameObject(typeof(T).Name);
                        mInstance = go.AddComponent<T>();
                        GameObject parent = GameObject.Find("SingleTon");
                        if (parent == null)
                        {
                            parent = new GameObject("SingleTon");
                            GameObject.DontDestroyOnLoad(parent);
                        }
                        if (parent != null)
                        {
                            go.transform.parent = parent.transform;
                        }
                    }
                }
    
                return mInstance;
            }
        }
    
        public static bool HasInstance
        {
            get { return mInstance != null; }
        }
    
        /*
         * 没有任何实现的函数，用于保证MonoSingleton在使用前已创建
         */
        public void Startup()
        {
    
        }
    
        private void Awake()
        {
            if (mInstance == null)
            {
                mInstance = this as T;
            }
    
            DontDestroyOnLoad(gameObject);
            Init();
        }
    
        protected virtual void Init()
        {
    
        }
    
        public void DestroySelf()
        {
            Dispose();
            mInstance = null;
            Destroy(gameObject);
        }
    
        public virtual void Dispose()
        {
    
        }
    
    }
}