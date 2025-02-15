﻿// Copyright (c) 2019 Timothé Lapetite - nebukam@gmail.com
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
using Nebukam.JobAssist;
using Unity.Collections;




namespace Nebukam.ORCA
{
    public class AgentKDTreeProcessor : Processor<AgentKDTreeJob>, IAgentKDTreeProvider
    {
        /// 
        /// Fields
        ///
        protected IAgentProvider                m_agentProvider;
        protected NativeArray<AgentTreeNode>    m_outputTree = new NativeArray<AgentTreeNode>(0, Allocator.Persistent);

        /// 
        /// Properties
        ///
        public IAgentProvider                   agentProvider { get { return m_agentProvider; } }
        public NativeArray<AgentTreeNode>       outputTree { get { return m_outputTree; } }

        protected override void                 InternalLock() { }
        protected override void                 InternalUnlock() { }

        protected override void Prepare(ref AgentKDTreeJob job, float delta)
        {
            if (!TryGetFirstInGroup(out m_agentProvider))
            {
                throw new System.Exception("No IAgentProvider in chain !");
            }

            int agentCount      = 2 * m_agentProvider.outputAgents.Length;
            if (m_outputTree.Length != agentCount)
            {
                m_outputTree.Dispose();
                m_outputTree = new NativeArray<AgentTreeNode>(agentCount, Allocator.Persistent);
            }

            job.m_inputAgents   = m_agentProvider.outputAgents;
            job.m_outputTree    = m_outputTree;

        }

        protected override void Apply(ref AgentKDTreeJob job)
        {

        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) { return; }

            m_outputTree.Dispose();
        }
    }
}
