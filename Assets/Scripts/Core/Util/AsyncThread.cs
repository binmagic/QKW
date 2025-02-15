﻿using System;
using System.Threading;

namespace Solarmax
{

    public class AsyncThread
    {
        public enum ThreadStatus
        {
            NONE,
            START,
            WORKING,
            STOP,
            FINISHED,
        }

        private volatile ThreadStatus mStatus; // Cause there only one method to change this value, no need to lock.
        private Callback<AsyncThread> mContext;
        private Callback mFinishedContext;
        private Thread mThread;
        private object mExtraData;

        public AsyncThread(Callback<AsyncThread> cb)
        {
            mStatus = ThreadStatus.NONE;
            mContext = cb;
            mFinishedContext = null;
            mThread = new Thread(new ThreadStart(ContextMask));
            mExtraData = null;
        }

        public AsyncThread(Callback<AsyncThread> cb, object extraData)
        {
            mStatus = ThreadStatus.NONE;
            mContext = cb;
            mFinishedContext = null;
            mThread = new Thread(new ThreadStart(ContextMask));
            mExtraData = extraData;
        }

        /**
         * This function may cause a little perfermance problem.
         * */
        private void ContextMask()
        {
            mStatus = ThreadStatus.WORKING;
            mContext(this);

            if (mStatus == ThreadStatus.STOP)
            {
                if (null != mFinishedContext)
                {
                    mFinishedContext();
                }
			}

			mStatus = ThreadStatus.FINISHED;
        }

        private void Release()
        {
            mContext = null;
            mFinishedContext = null;
            mThread = null;
            mExtraData = null;
        }

        public bool Start()
        {
            if (null != mThread)
            {
                mStatus = ThreadStatus.START;
                mThread.Start();

                return true;
            }

            return false;
        }

        public void Stop()
        {
            if (IsWorking())
            {
                mFinishedContext = null;
                mStatus = ThreadStatus.STOP;
            }
        }

        /**
         * cb will be called in async-thread
         * */
        public void AsyncStop(Callback cb)
        {
            if (IsWorking())
            {
                mFinishedContext = cb;
                mStatus = ThreadStatus.STOP;
            }
        }

        /**
         * cb will be called in sync-thread
         * */
        public void SyncStop(Callback cb)
        {
            if (IsWorking())
            {
                mFinishedContext = null;
                mStatus = ThreadStatus.STOP;

                while (mStatus != ThreadStatus.FINISHED) ;
                cb();
            }
            
        }

        public bool IsWorking()
        {
            return mStatus == ThreadStatus.WORKING;
        }

        public int GetThreadId()
        {
            return null == mThread ? -1 : mThread.ManagedThreadId;
        }

        public void SetExtraData(object data)
        {
            mExtraData = data;
        }

        public object GetExtraData()
        {
            return mExtraData;
        }
    }
}
