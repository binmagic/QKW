﻿using System;
using Unity.Collections;
using Unity.Jobs;

namespace Nebukam.JobAssist
{

    public interface IParallelProcessor : IProcessor
    {

        int chunkSize { get; set; }

    }


    public abstract class ParallelProcessor<T> : IParallelProcessor
        where T : struct, IJobParallelFor
    {

        public float deltaMultiplier { get; set; } = 1.0f;

        protected bool m_locked = false;
        public bool locked { get { return m_locked; } }

        public int chunkSize { get; set; } = 64;

        protected bool m_hasJobHandleDependency = false;
        protected JobHandle m_jobHandleDependency = default(JobHandle);

        public IProcessorGroup m_group = null;
        public IProcessorGroup group { get { return m_group; } set { m_group = value; } }

        public int groupIndex { get; set; } = -1;

        protected IProcessor m_procDependency = null;
        public IProcessor procDependency { get { return m_procDependency; } }

        protected T m_currentJob;
        protected JobHandle m_currentHandle;
        public JobHandle currentHandle { get { return m_currentHandle; } }

        protected float m_deltaSum = 0f;

        protected bool m_scheduled = false;
        public bool scheduled { get { return m_scheduled; } }
        public bool completed { get { return m_scheduled ? m_currentHandle.IsCompleted : false; } }

#if UNITY_EDITOR
        protected bool m_disposed = false;
#endif
        public virtual void Run(float delta)
        {

#if UNITY_EDITOR
            if (m_disposed)
            {
                throw new Exception("Schedule() called on disposed JobHandler ( " + GetType().Name + " ).");
            }
#endif
            m_deltaSum += delta;

            m_currentJob = default;
            int jobLength = Prepare(ref m_currentJob, m_deltaSum * deltaMultiplier);
            Lock();
            m_currentJob.Run(jobLength);

        }
        /// <summary>
        /// Schedule this job, with an optional dependency.
        /// </summary>
        /// <param name="delta"></param>
        /// <param name="dependsOn"></param>
        /// <returns></returns>
        public virtual JobHandle Schedule(float delta, IProcessor dependsOn = null)
        {

#if UNITY_EDITOR
            if (m_disposed)
            {
                throw new Exception("Schedule() called on disposed JobHandler ( " + GetType().Name + " ).");
            }
#endif
            m_deltaSum += delta;

            if (m_scheduled) { return m_currentHandle; }

            m_scheduled = true;
            m_hasJobHandleDependency = false;

            m_currentJob = default;

            Lock();
            int jobLength = Prepare(ref m_currentJob, m_deltaSum * deltaMultiplier);

            if (dependsOn != null)
            {
                m_procDependency = dependsOn;
                m_currentHandle = m_currentJob.Schedule(jobLength, chunkSize, m_procDependency.currentHandle);
            }
            else
            {
                m_procDependency = null;
                m_currentHandle = m_currentJob.Schedule(jobLength, chunkSize);
            }

            return m_currentHandle;
        }

        /// <summary>
        /// Schedule this job, with a JobHandle dependency.
        /// </summary>
        /// <param name="delta"></param>
        /// <param name="dependsOn"></param>
        /// <returns></returns>
        /// <remark>
        /// This method is provided to support integration in regular Unity's Job System workflow
        /// </remark>
        public virtual JobHandle Schedule(float delta, JobHandle dependsOn)
        {

#if UNITY_EDITOR
            if (m_disposed)
            {
                throw new Exception("Schedule() called on disposed JobHandler ( " + GetType().Name + " ).");
            }
#endif
            m_deltaSum += delta;

            if (m_scheduled) { return m_currentHandle; }

            m_scheduled = true;
            m_hasJobHandleDependency = true;
            m_procDependency = null;

            m_currentJob = default;

            Lock();
            int jobLength = Prepare(ref m_currentJob, m_deltaSum * deltaMultiplier);

            m_currentHandle = m_currentJob.Schedule(jobLength, chunkSize, dependsOn);

            return m_currentHandle;
        }

        protected abstract int Prepare(ref T job, float delta);

        public void RunComplete()
        {
            Apply(ref m_currentJob);
            Unlock();

            m_deltaSum = 0f;
        }

        /// <summary>
        /// Complete the job.
        /// </summary>
        public void Complete()
        {

#if UNITY_EDITOR
            if (m_disposed)
            {
                throw new Exception("Complete() called on disposed JobHandler ( " + GetType().Name + " ).");
            }
#endif

            if (!m_scheduled) { return; }

            if (m_hasJobHandleDependency)
                m_jobHandleDependency.Complete();

            m_procDependency?.Complete();
            m_currentHandle.Complete();

            m_scheduled = false;

            Apply(ref m_currentJob);
            Unlock();

            m_deltaSum = 0f;

        }

        public bool TryComplete()
        {
            if (!m_scheduled) { return false; }
            if (completed)
            {
                Complete();
                return true;
            }
            else
            {
                return false;
            }
        }

        protected abstract void Apply(ref T job);

        #region ILockable

        public void Lock()
        {
            if (m_locked) { return; }
            m_locked = true;
            InternalLock();
        }

        protected abstract void InternalLock();

        public void Unlock()
        {
            if (!m_locked) { return; }
            m_locked = false;
            //Complete the job for safety
            if (m_scheduled) { Complete(); }
            InternalUnlock();

        }

        protected abstract void InternalUnlock();

        #endregion

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) { return; }
#if UNITY_EDITOR
            m_disposed = true;
#endif

            //Complete the job first so we can rid of unmanaged resources.
            if (m_scheduled) { m_currentHandle.Complete(); }

            m_procDependency = null;
            m_scheduled = false;
        }

        public void Dispose()
        {
            // Dispose of unmanaged resources.
            Dispose(true);
            // Suppress finalization.
            GC.SuppressFinalize(this);
        }

        #region utils

        protected bool TryGetFirstInGroup<P>(out P processor, bool deep = false)
            where P : class, IProcessor
        {
            processor = null;
            if (m_group != null)
            {
                return m_group.TryGetFirst(groupIndex - 1, out processor, deep);
            }
            else
            {
                return false;
            }
        }

        #endregion

    }

}
