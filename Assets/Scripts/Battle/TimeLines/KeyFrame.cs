﻿using System;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;




namespace TimeLines
{
    public class KeyFrame
    {
        public float                Time;
        public ActionArgs           Args;

        public bool                 exed { get; set; }

        public List<KeyFrameArgs>   FramesActions;

        private float               _duration;

        public KeyFrame( float time, ActionArgs args )
        {
            Time            = time;
            Args            = args;
            FramesActions   = new List<KeyFrameArgs>();
        }

        public virtual void Tick( float duration )
        {
            _duration       = duration;
            if( !exed && duration >= Time )
            {
                for (int i = 0; i < FramesActions.Count; i++)
                    Execute(FramesActions[i]);
                exed = true;
            }
        }

        public virtual void Execute( KeyFrameArgs args )
        {

        }

        public void Over()
        {
            for (int i = 0; i < FramesActions.Count; i++)
                Stop(FramesActions[i]);
        }

        public virtual void ReSet( ActionArgs args )
        {
            Args        = args;
            exed        = false;
        }


        public virtual void Stop( KeyFrameArgs action )
        {

        }

        public virtual void BuildActions( TimeLine.Type tType, XmlNode xFrame )
        {
            var xActions        = xFrame.SelectNodes("Action");
            if( xActions != null )
            {
                for( int i = 0; i < xActions.Count; i++ )
                {
                    var xAction     = xActions[i];
                    var exportArgs  = typeof(KeyFrameArgs).Assembly.CreateInstance(tType + "KeyFrameExportArgs");
                    if( exportArgs != null )
                    {
                        var fs = exportArgs.GetType().GetFields();
                        for( int n = 0; n < fs.Length; n++ )
                        {
                            var fAttr = xAction.Attributes;
                            if (fAttr[fs[n].Name] == null) continue;
                            switch( fAttr[fs[n].Name].Name )
                            {
                                case "Operation":
                                    fs[n].SetValue(exportArgs, Enum.Parse(typeof(KeyFrameArgs.OperationType), fAttr[fs[n].Name].Value));
                                    break;
                            }
                            if (fs[n].FieldType == typeof(int))
                            {
                                fs[n].SetValue(exportArgs, int.Parse(fAttr[fs[n].Name].Value));
                            }
                            else if (fs[n].FieldType == typeof(float))
                            {
                                fs[n].SetValue(exportArgs, float.Parse(fAttr[fs[n].Name].Value));
                            }
                            else if (fs[n].FieldType == typeof(string))
                            {
                                fs[n].SetValue(exportArgs, fAttr[fs[n].Name].Value);
                            }
                            else if (fs[n].FieldType == typeof(bool))
                            {
                                fs[n].SetValue(exportArgs, fAttr[fs[n].Name].Value != "False" ? true : false);
                            }
                            else if (fs[n].FieldType == typeof(Vector3))
                            {
                                var vs = fAttr[fs[n].Name].Value.Split(',');
                                fs[n].SetValue(exportArgs, new Vector3(float.Parse(vs[0]), float.Parse(vs[1]), float.Parse(vs[2])));
                            }
                        }
                    }
                    FramesActions.Add(exportArgs as KeyFrameArgs );
                }
            }
        }
    }

    /// <summary>
    /// 动作
    /// </summary>
    public class ActionKeyFrame : KeyFrame
    {
        public ActionKeyFrame( float time, ActionArgs args ) : base ( time, args )
        {

        }

        public override void Tick(float duration)
        {
            base.Tick(duration);
        }

        public override void Execute(KeyFrameArgs args)
        {
            var kArgs   = args as ActionKeyFrameExportArgs;
            if( kArgs != null && Args.Source != null )
            {
                if( kArgs.Operation == KeyFrameArgs.OperationType.TurnOn )
                {
                    if( Args.Source.isALive )
                    {
                        float speed     = Args.TimeScale;
                        var aNames      = kArgs.ActionName.Split('.');
                        var nameIndex   = UnityEngine.Random.Range(0, aNames.Length);

                        /// 释放动作
                        Args.Source.entity.PlayAction(int.Parse(aNames[nameIndex]), speed );

                        /// 增加延迟处理逻辑
                        Args.Source.ActonTicks.AddDelayAction(kArgs.ActionFileLegth / speed, () =>
                        {
                            Args.Source.entity.SetAniSpeed( 1.0f );
                            if (Args != null && Args.OnActionFinishd != null)
                                Args.OnActionFinishd();
                        });
                    }
                    else
                    {

                    }
                }
                base.Execute(args);
            }
        }
    }


    /// <summary>
    /// 相机震动
    /// </summary>
    public class ShakeKeyFrame : KeyFrame
    {
        public ShakeKeyFrame( float time, ActionArgs args ) : base( time, args )
        {

        }


        public override void Execute( KeyFrameArgs args)
        {

        }
    }


    /// <summary>
    /// 声音
    /// </summary>
    public class SoundKeyFrame : KeyFrame
    {
        public SoundKeyFrame( float time, ActionArgs args ) : base( time, args )
        {

        }

        public override void Execute(KeyFrameArgs args)
        {
            var kArgs = args as SoundKeyFrameExportArgs;
            if( Args.Source != null )
            {
                var paths   = kArgs.SoundPath.Split(',');
                var sel     = UnityEngine.Random.Range( 0, paths.Length );
                AudioManger.Get().PlayEffect(paths[sel] );
            }
            base.Execute(args);
        }

        public override void Stop(KeyFrameArgs action)
        {
            base.Stop(action);
        }
    }


    public class EffectKeyFrame : KeyFrame
    {
        private EffectKeyFrameExportArgs    kArgs = null;
        public EffectKeyFrame( float time, ActionArgs args ) : base( time, args )
        {

        }


        public override void Execute( KeyFrameArgs args)
        {
            kArgs = args as EffectKeyFrameExportArgs;
            if( Args.Source != null )
            {
                if( kArgs.Operation == KeyFrameArgs.OperationType.TurnOff )
                {

                }
                else if ( kArgs.Operation == KeyFrameArgs.OperationType.TurnOn )
                {
                    Vector3 bornPos     = Args.Source.GetPosition();
                    if( kArgs.ZeroPos == false )
                    {
                        bornPos = Args.Source.entity.GetBonePoint( kArgs.BonePoint ).position;
                    }
                    else
                    {
                        bornPos = string.IsNullOrEmpty( kArgs.BonePoint) ? Args.Source.GetPosition()
                                                                         : Args.Source.entity.GetBonePoint(kArgs.BonePoint).position;
                    }

                    EffectManager.Get().PlayParticleEffect(bornPos, Quaternion.identity, kArgs.EffectName, kArgs.LifeTime );
                }
            }
            base.Execute(args);
        }


        public override void Stop(KeyFrameArgs action)
        {
            if( kArgs != null )
            {

            }
            base.Stop(action);
        }
    }
}
