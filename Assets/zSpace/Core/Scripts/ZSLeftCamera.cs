//////////////////////////////////////////////////////////////////////////
//
//  Copyright (C) 2007-2014 zSpace, Inc.  All Rights Reserved.
//
//  File:       ZSLeftCamera.cs
//  Content:    The zSpace left camera script.
//
//////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System;
using System.Collections;
using System.Runtime.InteropServices;

public class ZSLeftCamera : MonoBehaviour
{
    void Start()
    {
        _core      = GameObject.FindObjectOfType(typeof(ZSCore)) as ZSCore;
        _stereoRig = GameObject.Find("ZSStereoRig");
    }

    void OnPreCull()
    {
        if (_core != null)
        {
            // Specify the correct eye.
            ZSCore.Eye eye = ZSCore.Eye.Left;

            if (_core.AreEyesSwapped())
                eye = ZSCore.Eye.Right;

            if (_core.CurrentCamera != null && _stereoRig != null)
            {
                // Grab the current monoscopic camera's transform and apply it
                // to the ZSStereoRig.
                _stereoRig.transform.position   = _core.CurrentCamera.transform.position;
                _stereoRig.transform.rotation   = _core.CurrentCamera.transform.rotation;
                _stereoRig.transform.localScale = _core.CurrentCamera.transform.localScale;
            }

            // Calculate left camera's transform.
            Matrix4x4 viewMatrixInverse = ZSCore.ConvertFromRightToLeft(_core.GetViewMatrix(eye).inverse);
            transform.localPosition = viewMatrixInverse.GetColumn(3);
            transform.localRotation = Quaternion.LookRotation(viewMatrixInverse.GetColumn(2), viewMatrixInverse.GetColumn(1));

            // Set the left camera's projection matrix.
            gameObject.camera.projectionMatrix = _core.GetProjectionMatrix(eye);
        }

        GL.IssuePluginEvent((int)ZSCore.GlPluginEventType.RenderTargetLeft);
    }

    private ZSCore     _core        = null;
    private GameObject _stereoRig   = null;
}
