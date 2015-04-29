//////////////////////////////////////////////////////////////////////////
//
//  Copyright (C) 2007-2014 zSpace, Inc.  All Rights Reserved.
//
//  File:       ZSCore.cs
//  Content:    The zSpace Core Interface for Unity.
//
//////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class ZSCore : MonoBehaviour
{
    #region ENUMS

    public enum GlPluginEventType
    {
        RenderTargetLeft         = 10000,
        RenderTargetRight        = 10001,
        FrameDone                = 10002,
        DisableStereo            = 10003,
        InitializeLRDetect       = 10004,
        UpdateLRDetectFullscreen = 10005,
        UpdateLRDetectWindowed   = 10006,
        SyncLRDetectFullscreen   = 10007,
        SyncLRDetectWindowed     = 10008
    }

    public enum PluginError
    {
        Okay                  = 0,
        NotImplemented        = 1,
        NotInitialized        = 2,
        AlreadyInitialized    = 3,
        InvalidParameter      = 4,
        InvalidContext        = 5,
        InvalidHandle         = 6,
        RuntimeIncompatible   = 7,
        RuntimeNotFound       = 8,
        SymbolNotFound        = 9,
        DisplayNotFound       = 10,
        DeviceNotFound        = 11,
        TargetNotFound        = 12,
        CapabilityNotFound    = 13,
        BufferTooSmall        = 14
    }

    public enum CoordinateSpace
    {
        Tracker  = 0,
        Display  = 1,
        Viewport = 2,
        Camera   = 3
    }

    public enum FrustumAttribute
    {
        Ipd           = 0,
        ViewerScale   = 1,
        FovScale      = 2,
        HeadScale     = 3,
        NearClip      = 4,
        FarClip       = 5,
        GlassesOffset = 6,
        CCLimit       = 7,
        UCLimit       = 8,
        CULimit       = 9,
        UULimit       = 10,
        CCDepth       = 11,
        UCDepth       = 12
    }

    public enum PortalMode
    {
        None     =  0,
        Angle    =  1,
        Position =  2,
        All      = ~0
    }

    public enum Eye
    {
        Left   = 0,
        Right  = 1,
        Center = 2,
        NumEyes
    }

    public enum CameraType
    {
        Left  = 0,
        Right = 1,
        Final = 2,
        NumTypes
    }

    public enum TrackerTargetType
    {
        Unknown   = -1,
        Head      =  0,
        Primary   =  1,
        Secondary =  2,
        NumTypes
    }

    public enum LedColor
    {
        Black   = 0,
        White   = 1,
        Red     = 2,
        Green   = 3,
        Blue    = 4,
        Cyan    = 5,
        Magenta = 6,
        Yellow  = 7
    }

    public enum MouseMovementMode
    {
        Absolute = 0,
        Relative = 1
    }

    public enum MouseButton
    {
        Unknown = -1,
        Left    =  0,
        Right   =  1,
        Center  =  2
    }

    #endregion


    #region STRUCTS

    public struct DisplayIntersectionInfo
    {
        public bool  hit;
        public int   x;
        public int   y;
        public int   nx;
        public int   ny;
        public float distance;
    }

    #endregion


    #region UNITY EDITOR

    public GameObject CurrentCamera  = null;

    public bool EnableStereo         = true;
    public bool EnableTracking       = true;
    public bool EnableMouseEmulation = false;

    [Range(0.01f, 0.2f)]
    public float InterPupillaryDistance = 0.06f;

    [Range(0.01f, 1000.0f)]
    public float ViewerScale = 1;

    [Range(0.01f, 1000.0f)]
    public float FieldOfViewScale = 1;

    [Range(0, 1)]
    public float HeadTrackingScale = 1;

    [Range(0.0f, 0.2f)]
    public float GlassesOffset = 0.01f;

    #endregion


    #region UNITY CALLBACKS

    void Awake()
    {
        // Grab the ZSCoreSingleton and verify that it is initialized.
        _coreSingleton = ZSCoreSingleton.Instance;

        // If the CurrentCamera is null, default to Camera.main.
        if (!this.IsCurrentCameraValid() && Camera.main != null)
            this.CurrentCamera = Camera.main.gameObject;

        // Initialization.
        this.Initialize();
        this.InitializeStereoCameras();
        this.CheckForUpdates();
        _wasFullScreen = Screen.fullScreen;

        // Temporarily re-enable the camera in case other MonoBehaviour scripts
        // want to reference Camera.main in their Awake() method.
        if (this.IsCurrentCameraValid())
            this.CurrentCamera.camera.enabled = true;

        if (_coreSingleton.IsInitialized)
        {
            // Set the window size.
            zsupSetViewportSize(Screen.width, Screen.height);

            // Start the update coroutine.
            StartCoroutine("UpdateCoroutine");
        }
    }

    void OnDestroy()
    {
        // Stop the update coroutine.
        StopCoroutine("UpdateCoroutine");

        if (_coreSingleton.IsInitialized)
        {
            GL.IssuePluginEvent((int)ZSCore.GlPluginEventType.DisableStereo);
            GL.InvalidateState();
        }
    }

    void LateUpdate()
    {
        if (this.IsStereoEnabled())
            this.CurrentCamera.camera.enabled = false;
    }

    #endregion


    #region ZSPACE APIS

    /// <summary>
    /// Set whether or not stereoscopic 3D is enabled.
    /// </summary>
    /// <param name="isStereoEnabled">True to enable stereoscopic 3D.  False otherwise.</param>
    public void SetStereoEnabled(bool isStereoEnabled)
    {
        if (!_coreSingleton.IsInitialized)
            return;

        if (this.IsCurrentCameraValid())
        {
            this.CurrentCamera.camera.enabled = !isStereoEnabled;

            if (_stereoCameras[(int)CameraType.Left] != null)
                _stereoCameras[(int)CameraType.Left].enabled = isStereoEnabled;

            if (_stereoCameras[(int)CameraType.Right] != null)
                _stereoCameras[(int)CameraType.Right].enabled = isStereoEnabled;

            this.EnableStereo = isStereoEnabled;
            _isStereoEnabled  = isStereoEnabled;
        }
    }

    /// <summary>
    /// Check whether or not stereoscopic 3D rendering is enabled.
    /// </summary>
    /// <returns>True if stereoscopic 3D is enabled.  False if not.</returns>
    public bool IsStereoEnabled()
    {
        return _isStereoEnabled;
    }

    /// <summary>
    /// Set whether or not tracking is enabled.
    /// </summary>
    /// <param name="isEnabled">Whether or not tracking is enabled.</param>
    public void SetTrackingEnabled(bool isEnabled)
    {
        this.EnableTracking = isEnabled;
        zsupSetTrackingEnabled(isEnabled);
    }

    /// <summary>
    /// Check whether or not tracking is enabled.
    /// </summary>
    /// <returns>True is tracking is enabled, false if not.</returns>
    public bool IsTrackingEnabled()
    {
        bool isTrackingEnabled = false;
        zsupIsTrackingEnabled(out isTrackingEnabled);
        return isTrackingEnabled;
    }

    /// <summary>
    /// Set whether or not the left and right eyes are swapped.
    /// </summary>
    /// <param name="areEyesSwapped">Whether or not the left and right eyes are swapped.</param>
    public void SetEyesSwapped(bool areEyesSwapped)
    {
        _areEyesSwapped = areEyesSwapped;
    }

    /// <summary>
    /// Check whether or not the left and right eyes are swapped.
    /// </summary>
    /// <returns>Whether or not the left and right eyes are swapped.</returns>
    public bool AreEyesSwapped()
    {
        return _areEyesSwapped;
    }

    /// <summary>
    /// Get the virtual (x, y) position of the current display.
    /// </summary>
    /// <returns>The display position (virtual x, y coordinates) in Vector2 format.</returns>
    public Vector2 GetDisplayPosition()
    {
        int x = 0;
        int y = 0;
        zsupGetDisplayPosition(out x, out y);
        return new Vector2(x, y);
    }

    /// <summary>
    /// Get the size of the current display.
    /// </summary>
    /// <returns>The display size (in meters) in Vector2 format.</returns>
    public Vector2 GetDisplaySize()
    {
        Vector2 displaySize = new Vector2();
        zsupGetDisplaySize(out displaySize.x, out displaySize.y);
        return displaySize;
    }

    /// <summary>
    /// Get the resolution of the current display.
    /// </summary>
    /// <returns>The display resolution (in pixels) in Vector2 format.</returns>
    public Vector2 GetDisplayResolution()
    {
        int x = 0;
        int y = 0;
        zsupGetDisplayResolution(out x, out y);
        return new Vector2(x, y);
    }

    /// <summary>
    /// Get the angle of the current display.
    /// </summary>
    /// <returns>The display angle (in degrees) in Vector2 format.</returns>
    public Vector3 GetDisplayAngle()
    {
        Vector3 displayAngle = new Vector3();
        zsupGetDisplayAngle(out displayAngle.x, out displayAngle.y, out displayAngle.z);
        return displayAngle;
    }
    
    /// <summary>
    /// DEPRECATED: Use GetCameraOffset() instead.
    /// Get the display offset.
    /// </summary>
    /// <returns></returns>
    public Vector3 GetDisplayOffset()
    {
        return -(this.GetCameraOffset());
    }

    /// <summary>
    /// Check whether or not the display hardware is present (USB connected).
    /// </summary>
    /// <returns>True if the hardware is present. False otherwise.</returns>
    public bool IsDisplayHardwarePresent()
    {
        bool isHardwarePresent = false;
        zsupIsDisplayHardwarePresent(out isHardwarePresent);
        return isHardwarePresent;
    }

    /// <summary>
    /// Perform a raycast against the zSpace display given
    /// a specified tracker space pose.
    /// </summary>
    /// <param name="pose">Pose in tracker space.</param>
    /// <returns>Display intersection info.</returns>
    public DisplayIntersectionInfo IntersectDisplay(Matrix4x4 pose)
    {
        DisplayIntersectionInfo info = new DisplayIntersectionInfo();
        zsupIntersectDisplay(this.ConvertToFloatArray(pose), out info.hit, out info.x, out info.y, out info.nx, out info.ny, out info.distance);
        return info;
    }

    /// <summary>
    /// Get the viewport's top-left corner in pixels.
    /// </summary>
    /// <param name="x">Left-most position.</param>
    /// <param name="y">Top-most position.</param>
    public void GetViewportPosition(out int x, out int y)
    {
        zsupGetViewportPosition(out x, out y);
    }

    /// <summary>
    /// Get the viewport's size in pixels.
    /// </summary>
    /// <param name="width">Viewport width.</param>
    /// <param name="height">Viewport height.</param>
    public void GetViewportSize(out int width, out int height)
    {
        zsupGetViewportSize(out width, out height);
    }
    
    /// <summary>
    /// DEPRECATED: Use GetCoordinateSpaceTransform() instead.
    /// Get the viewport offset.
    /// </summary>
    /// <returns></returns>
    public Vector3 GetViewportOffset()
    {
        Matrix4x4 displayToViewportSpace = this.GetCoordinateSpaceTransform(CoordinateSpace.Display, CoordinateSpace.Viewport);
        return displayToViewportSpace.GetColumn(3);
    }    

    /// <summary>
    /// Get the transformation matrix from coordinate space a to b.
    /// </summary>
    /// <param name="a">Coordinate space a.</param>
    /// <param name="b">Coordinate space b</param>
    /// <returns>Coordinate space transformation matrix.</returns>
    public Matrix4x4 GetCoordinateSpaceTransform(CoordinateSpace a, CoordinateSpace b)
    {
        float[] transformData = new float[16];
        zsupGetCoordinateSpaceTransform((int)a, (int)b, transformData);
        return this.ConvertToMatrix4x4(transformData);
    }
    
    /// <summary>
    /// DEPRECATED: Use GetCoordinateSpaceTransform() instead.
    /// Get the tracker to camera space transform.
    /// </summary>
    /// <returns></returns>
    public Matrix4x4 GetTrackerToCameraSpaceTransform()
    {
        return this.GetCoordinateSpaceTransform(CoordinateSpace.Tracker, CoordinateSpace.Camera);
    }

    /// <summary>
    /// Set the inter-pupillary distance - the physical distance between the user's eyes.
    /// </summary>
    /// <param name="interPupillaryDistance">The inter-pupillary distance (in meters).</param>
    public void SetInterPupillaryDistance(float interPupillaryDistance)
    {
        this.InterPupillaryDistance = interPupillaryDistance;
        zsupSetFrustumAttribute((int)FrustumAttribute.Ipd, interPupillaryDistance);
    }

    /// <summary>
    /// Get the inter-pupillary distance - the physical distance between the user's eyes.
    /// </summary>
    /// <returns>The inter-pupillary distance (in meters).</returns>
    public float GetInterPupillaryDistance()
    {
        float interPupillaryDistance = 0.0f;
        zsupGetFrustumAttribute((int)FrustumAttribute.Ipd, out interPupillaryDistance);
        return interPupillaryDistance;
    }

    /// <summary>
    /// Set the viewer scale.
    /// </summary>
    /// <param name="worldScale">The viewer scale.</param>
    public void SetViewerScale(float viewerScale)
    {
        this.ViewerScale = viewerScale;
        zsupSetFrustumAttribute((int)FrustumAttribute.ViewerScale, viewerScale);
    }

    /// <summary>
    /// Get the viewer scale.
    /// </summary>
    /// <returns>The viewer scale.</returns>
    public float GetViewerScale()
    {
        float viewerScale = 1.0f;
        zsupGetFrustumAttribute((int)FrustumAttribute.ViewerScale, out viewerScale);
        return viewerScale;
    }

    /// <summary>
    /// Set the field of view scale.
    /// </summary>
    /// <param name="fieldOfViewScale">The field of view scale.</param>
    public void SetFieldOfViewScale(float fieldOfViewScale)
    {
        this.FieldOfViewScale = fieldOfViewScale;
        zsupSetFrustumAttribute((int)FrustumAttribute.FovScale, fieldOfViewScale);
    }

    /// <summary>
    /// Get the field of view scale.
    /// </summary>
    /// <returns>The field of view scale.</returns>
    public float GetFieldOfViewScale()
    {
        float fieldOfViewScale = 1.0f;
        zsupGetFrustumAttribute((int)FrustumAttribute.FovScale, out fieldOfViewScale);
        return fieldOfViewScale;
    }

    /// <summary>
    /// Set the uniform scale that is to be applied to the head tracked position.
    /// </summary>
    /// <param name="headTrackingScale">The scale applied to head tracking.</param>
    public void SetHeadTrackingScale(float headTrackingScale)
    {
        this.HeadTrackingScale = headTrackingScale;
        zsupSetFrustumAttribute((int)FrustumAttribute.HeadScale, headTrackingScale);
    }

    /// <summary>
    /// Get the uniform scale that is applied to the head tracked position.
    /// </summary>
    /// <returns>The scale applied to head tracking.</returns>
    public float GetHeadTrackingScale()
    {
        float headTrackingScale = 0.0f;
        zsupGetFrustumAttribute((int)FrustumAttribute.HeadScale, out headTrackingScale);
        return headTrackingScale;
    }

    /// <summary>
    /// Set the glasses offset for the stereo frustum.
    /// </summary>
    /// <param name="glassesOffset">The distance between the bridge of the glasses and the bridge of the nose in meters.</param>
    public void SetGlassesOffset(float glassesOffset)
    {
        this.GlassesOffset = glassesOffset;
        zsupSetFrustumAttribute((int)FrustumAttribute.GlassesOffset, glassesOffset);
    }

    /// <summary>
    /// Get the glasses offset for the stereo frustum.
    /// </summary>
    /// <returns>The distance between the bridge of the glasses and the bridge of the nose in meters.</returns>
    public float GetGlassesOffset()
    {
        float glassesOffset = 0.0f;
        zsupGetFrustumAttribute((int)FrustumAttribute.GlassesOffset, out glassesOffset);
        return glassesOffset;
    }

    /// <summary>
    /// Set the near clip distance.
    /// </summary>
    /// <param name="nearClip">The near clip distance (in meters).</param>
    public void SetNearClip(float nearClip)
    {
        zsupSetFrustumAttribute((int)FrustumAttribute.NearClip, nearClip);

        if (_stereoCameras[(int)CameraType.Left] != null)
            _stereoCameras[(int)CameraType.Left].nearClipPlane = nearClip;

        if (_stereoCameras[(int)CameraType.Right] != null)
            _stereoCameras[(int)CameraType.Right].nearClipPlane = nearClip;
    }

    /// <summary>
    /// Get the near clip distance.
    /// </summary>
    /// <returns>The near clip distance (in meters).</returns>
    public float GetNearClip()
    {
        float nearClip = 0.0f;
        zsupGetFrustumAttribute((int)FrustumAttribute.NearClip, out nearClip);
        return nearClip;
    }

    /// <summary>
    /// Set the far clip distance.
    /// </summary>
    /// <param name="farClip">The far clip distance (in meters).</param>
    public void SetFarClip(float farClip)
    {
        zsupSetFrustumAttribute((int)FrustumAttribute.FarClip, farClip);

        if (_stereoCameras[(int)CameraType.Left] != null)
            _stereoCameras[(int)CameraType.Left].farClipPlane = farClip;

        if (_stereoCameras[(int)CameraType.Right] != null)
            _stereoCameras[(int)CameraType.Right].farClipPlane = farClip;
    }

    /// <summary>
    /// Get the far clip distance.
    /// </summary>
    /// <returns>The far clip distance (in meters).</returns>
    public float GetFarClip()
    {
        float farClip = 0.0f;
        zsupGetFrustumAttribute((int)FrustumAttribute.FarClip, out farClip);
        return farClip;
    }

    /// <summary>
    /// Set the frustum's portal mode.
    /// </summary>
    /// <param name="portalModeFlags">A bitmask for the portal mode flags.</param>
    public void SetPortalMode(int portalModeFlags)
    {
        zsupSetFrustumPortalMode(portalModeFlags);
    }

    /// <summary>
    /// Get the frustum's portal mode.
    /// </summary>
    /// <returns>A bitmask for the portal mode flags.</returns>
    public int GetPortalMode()
    {
        int portalModeFlags = 0;
        zsupGetFrustumPortalMode(out portalModeFlags);
        return portalModeFlags;
    }

    /// <summary>
    /// Set the frustum's camera offset (distance of virtual camera
    /// to center of the viewport).
    /// </summary>
    /// <param name="cameraOffset">The desired camera offset in meters.</param>
    public void SetCameraOffset(Vector3 cameraOffset)
    {
        zsupSetFrustumCameraOffset(this.ConvertToFloatArray(cameraOffset));
    }

    /// <summary>
    /// Get the frustum's camera offset (distance of virtual camera
    /// to center of the viewport).
    /// </summary>
    /// <returns>The camera offset in meters.</returns>
    public Vector3 GetCameraOffset()
    {
        float[] cameraOffsetData = new float[3];
        zsupGetFrustumCameraOffset(cameraOffsetData);
        return this.ConvertToVector3(cameraOffsetData);
    }

    /// <summary>
    /// Get the view matrix for a specified eye.
    /// </summary>
    /// <param name="eye">The eye: left, right, or center.</param>
    /// <returns>The view matrix in Matrix4x4 format.</returns>
    public Matrix4x4 GetViewMatrix(Eye eye)
    {
        return _viewMatrices[(int)eye];
    }

    /// <summary>
    /// Get the projection matrix for a specified eye.
    /// </summary>
    /// <param name="eye">The eye: left, right, or center.</param>
    /// <returns>The projection matrix in Matrix4x4 format.</returns>
    public Matrix4x4 GetProjectionMatrix(Eye eye)
    {
        return _projectionMatrices[(int)eye];
    }

    /// <summary>
    /// Get the position of a specified eye.
    /// </summary>
    /// <param name="eye">The eye: left, right, or center.</param>
    /// <returns>The position of the eye in Vector3 format.</returns>
    public Vector3 GetEyePosition(Eye eye)
    {
        float[] positionData = new float[3];
        zsupGetFrustumEyePosition((int)eye, positionData);
        return this.ConvertToVector3(positionData);
    }

    /// <summary>
    /// Get the frustum bounds for a specified eye.
    /// </summary>
    /// <param name="eye">The eye: left, right, or center.</param>
    /// <param name="bounds">The frustum bounds corresponding to a specified eye laid out as follows:\n\n
    /// [left, right, bottom, top, nearClip, farClip]</param>
    public void GetFrustumBounds(Eye eye, float[/*6*/] bounds)
    {
        zsupGetFrustumBounds((int)eye, bounds);
    }

    /// <summary>
    /// Set whether or not a specified TrackerTarget is enabled.
    /// </summary>
    /// <param name="trackerTargetType">The type of TrackerTarget.</param>
    /// <param name="isEnabled">Whether or not to enable the target.</param>
    public void SetTrackerTargetEnabled(TrackerTargetType trackerTargetType, bool isEnabled)
    {
        zsupSetTargetEnabled((int)trackerTargetType, isEnabled);
    }

    /// <summary>
    /// Check whether or not a specified TrackerTarget is enabled.
    /// </summary>
    /// <param name="trackerTargetType">The type of TrackerTarget.</param>
    /// <returns>Whether or not the target is enabled.</returns>
    public bool IsTrackerTargetEnabled(TrackerTargetType trackerTargetType)
    {
        bool isEnabled = false;
        zsupIsTargetEnabled((int)trackerTargetType, out isEnabled);
        return isEnabled;
    }

    /// <summary>
    /// Check whether or not a specified TrackerTarget is visible.
    /// </summary>
    /// <param name="trackerTargetType">The type of TrackerTarget.</param>
    /// <returns>Whether or not the target is visible.</returns>
    public bool IsTrackerTargetVisible(TrackerTargetType trackerTargetType)
    {
        return _isTrackerTargetVisible[(int)trackerTargetType];
    }

    /// <summary>
    /// Get the tracker space pose of a specified default TrackerTarget.
    /// </summary>
    /// <param name="trackerTargetType">The type of TrackerTarget.</param>
    /// <returns>The Matrix4x4 pose in tracker space.</returns>
    public Matrix4x4 GetTrackerTargetPose(TrackerTargetType trackerTargetType)
    {
        return _trackerTargetPoses[(int)trackerTargetType];
    }

    /// <summary>
    /// Get the camera space pose of a specified default TrackerTarget.
    /// </summary>
    /// <param name="trackerTargetType">The type of TrackerTarget.</param>
    /// <returns>The Matrix4x4 pose in camera space.</returns>
    public Matrix4x4 GetTrackerTargetCameraPose(TrackerTargetType trackerTargetType)
    {
        return _trackerTargetCameraPoses[(int)trackerTargetType];
    }

    /// <summary>
    /// Get the world space pose of a specified default TrackerTarget.
    /// This forces a recalculation based on the current camera's local
    /// to world matrix.
    /// </summary>
    /// <param name="trackerTargetType">The type of TrackerTarget.</param>
    /// <returns>The Matrix4x4 pose in world space.</returns>
    public Matrix4x4 GetTrackerTargetWorldPose(TrackerTargetType trackerTargetType)
    {
        Matrix4x4 trackerTargetWorldPose = _trackerTargetCameraPoses[(int)trackerTargetType];

        // Scale the position based on world and field of view scales.
        trackerTargetWorldPose[0, 3] *= this.FieldOfViewScale;
        trackerTargetWorldPose[1, 3] *= this.FieldOfViewScale;
      
        // Convert the camera space pose to world space.
        if (this.IsCurrentCameraValid())
            trackerTargetWorldPose = this.CurrentCamera.transform.localToWorldMatrix * trackerTargetWorldPose;

        return trackerTargetWorldPose;
    }

    /// <summary>
    /// Get the cached world space pose of a specified default TrackerTarget.
    /// </summary>
    /// <param name="trackerTargetType">The type of TrackerTarget.</param>
    /// <returns>The Matrix4x4 pose in world space.</returns>
    public Matrix4x4 GetCachedTrackerTargetWorldPose(TrackerTargetType trackerTargetType)
    {
        return _trackerTargetWorldPoses[(int)trackerTargetType];
    }

    /// <summary>
    /// Set whether or not pose buffering is enabled for a specified TrackerTarget.
    /// </summary>
    /// <param name="trackerTargetType">The type of TrackerTarget.</param>
    /// <param name="isPoseBufferingEnabled">Flag denoting whether or not to enable pose buffering.</param>
    public void SetTrackerTargetPoseBufferingEnabled(TrackerTargetType trackerTargetType, bool isPoseBufferingEnabled)
    {
        zsupSetTargetPoseBufferingEnabled((int)trackerTargetType, isPoseBufferingEnabled);
    }

    /// <summary>
    /// Check whether or not pose buffering is enabled for a specified TrackerTarget.
    /// </summary>
    /// <param name="trackerTargetType">The type of TrackerTarget.</param>
    /// <returns>True if pose buffering is enabled.  False if not.</returns>
    public bool IsTrackerTargetPoseBufferingEnabled(TrackerTargetType trackerTargetType)
    {
        bool isPoseBufferingEnabled = false;
        zsupIsTargetPoseBufferingEnabled((int)trackerTargetType, out isPoseBufferingEnabled);
        return isPoseBufferingEnabled;
    }

    /// <summary>
    /// Get the tracker space buffered pose of a specified default TrackerTarget.
    /// </summary>
    /// <param name="trackerTargetType">The type of TrackerTarget.</param>
    /// <param name="lookBackTime"></param>
    /// <returns></returns>
    public Matrix4x4 GetTrackerTargetBufferedPose(TrackerTargetType trackerTargetType, float lookBackTime)
    {
        float[] matrixData = new float[16];
        double timestamp = 0.0;
        zsupGetTargetBufferedPose((int)trackerTargetType, lookBackTime, matrixData, out timestamp);
        return this.ConvertToMatrix4x4(matrixData);
    }

    /// <summary>
    /// Get the camera space buffered pose of a specified default TrackerTarget.
    /// </summary>
    /// <param name="trackerTargetType">The type of TrackerTarget.</param>
    /// <param name="lookBackTime"></param>
    /// <returns></returns>
    public Matrix4x4 GetTrackerTargetBufferedCameraPose(TrackerTargetType trackerTargetType, float lookBackTime)
    {
        float[] matrixData = new float[16];
        double timestamp = 0.0;
        zsupGetTargetBufferedCameraPose((int)trackerTargetType, lookBackTime, matrixData, out timestamp);

        Matrix4x4 trackerTargetCameraPose = ZSCore.ConvertFromRightToLeft(this.ConvertToMatrix4x4(matrixData));
        return trackerTargetCameraPose;
    }

    /// <summary>
    /// Get the world space buffered pose of a specified default TrackerTarget.
    /// </summary>
    /// <param name="trackerTargetType">The type of TrackerTarget.</param>
    /// <param name="lookBackTime"></param>
    /// <returns></returns>
    public Matrix4x4 GetTrackerTargetBufferedWorldPose(TrackerTargetType trackerTargetType, float lookBackTime)
    {
        float[] matrixData = new float[16];
        double timestamp = 0.0;
        zsupGetTargetBufferedCameraPose((int)trackerTargetType, lookBackTime, matrixData, out timestamp);

        Matrix4x4 trackerTargetWorldPose = ZSCore.ConvertFromRightToLeft(this.ConvertToMatrix4x4(matrixData));

        // Scale the position based on world and field of view scales.
        trackerTargetWorldPose[0, 3] *= this.FieldOfViewScale;
        trackerTargetWorldPose[1, 3] *= this.FieldOfViewScale;
      
        // Convert the camera space pose to world space.
        if (this.IsCurrentCameraValid())
            trackerTargetWorldPose = CurrentCamera.transform.localToWorldMatrix * trackerTargetWorldPose;

        return trackerTargetWorldPose;
    }

    /// <summary>
    /// Get the number of buttons associated with a specified TrackerTarget.
    /// </summary>
    /// <param name="trackerTargetType">The type of the TrackerTarget.</param>
    /// <returns>The number of buttons contained by a TrackerTarget.</returns>
    public int GetNumTrackerTargetButtons(TrackerTargetType trackerTargetType)
    {
        int numButtons = 0;
        zsupGetNumTargetButtons((int)trackerTargetType, out numButtons);
        return numButtons;
    }

    /// <summary>
    /// Check whether or not a specified target button is pressed.
    /// </summary>
    /// <param name="trackerTargetType">The type of TrackerTarget.</param>
    /// <param name="buttonId">The id of the button.</param>
    /// <returns>True if the button is pressed, false otherwise.</returns>
    public bool IsTrackerTargetButtonPressed(TrackerTargetType trackerTargetType, int buttonId)
    {
        bool isPressed = false;
        zsupIsTargetButtonPressed((int)trackerTargetType, buttonId, out isPressed);
        return isPressed;
    }

    /// <summary>
    /// Set whether or not the TrackerTarget's LED is enabled.
    /// </summary>
    /// <param name="trackerTargetType">The type of TrackerTarget.</param>
    /// <param name="isLedEnabled">Whether or not to enable the LED.</param>
    public void SetTrackerTargetLedEnabled(TrackerTargetType trackerTargetType, bool isLedEnabled)
    {
        zsupSetTargetLedEnabled((int)trackerTargetType, isLedEnabled);
    }
    
    /// <summary>
    /// Check whether or not the TrackerTarget's LED is enabled.
    /// </summary>
    /// <param name="trackerTargetType">The type of TrackerTarget.</param>
    /// <returns>Whether or not the LED is enabled.</returns>
    public bool IsTrackerTargetLedEnabled(TrackerTargetType trackerTargetType)
    {
        bool isLedEnabled = false;
        zsupIsTargetLedEnabled((int)trackerTargetType, out isLedEnabled);
        return isLedEnabled;
    }

    /// <summary>
    /// Check whether or not the TrackerTarget's LED is on.
    /// </summary>
    /// <param name="trackerTargetType">The type of TrackerTarget.</param>
    /// <returns>Whether or not the LED is on.</returns>
    public bool IsTrackerTargetLedOn(TrackerTargetType trackerTargetType)
    {
        bool isLedOn = false;
        zsupIsTargetLedOn((int)trackerTargetType, out isLedOn);
        return isLedOn;
    }

    /// <summary>
    /// Set the TrackerTarget's LED color.
    /// </summary>
    /// <param name="trackerTargetType">The type of TrackerTarget.</param>
    /// <param name="ledColor">The color of the LED.</param>
    public void SetTrackerTargetLedColor(TrackerTargetType trackerTargetType, LedColor ledColor)
    {
        int[] color = _ledColors[(int)ledColor];
        zsupSetTargetLedColor((int)trackerTargetType, color[0], color[1], color[2]);
    }


    /// <summary>
    /// Get the TrackerTarget's LED color.
    /// </summary>
    /// <param name="trackerTargetType">The type of TrackerTarget.</param>
    /// <returns>The color of the LED.</returns>
    public LedColor GetTrackerTargetLedColor(TrackerTargetType trackerTargetType)
    {
        float r = 0;
        float g = 0;
        float b = 0;
        zsupGetTargetLedColor((int)trackerTargetType, out r, out g, out b);

        for (int i = 0; i < _ledColors.Count; ++i)
        {
            int[] color = _ledColors[i];

            if ((int)r == color[0] && (int)g == color[1] && (int)b == color[2])
                return (LedColor)i;
        }

        return LedColor.Black;
    }

    /// <summary>
    /// Set whether or not the TrackerTarget's vibration is enabled.  This only determines
    /// whether the appropriate command is sent to the hardware if StartTrackerTargetVibration()
    /// is called.  If the TrackerTarget is already vibrating, StopTrackerTargetVibration() should
    /// be called to stop the current vibration.
    /// </summary>
    /// <param name="trackerTargetType">The type of TrackerTarget.</param>
    /// <param name="isVibrationEnabled"></param>
    public void SetTrackerTargetVibrationEnabled(TrackerTargetType trackerTargetType, bool isVibrationEnabled)
    {
        zsupSetTargetVibrationEnabled((int)trackerTargetType, isVibrationEnabled);
    }

    /// <summary>
    /// Check whether or not TrackerTarget vibration is enabled.
    /// </summary>
    /// <param name="trackerTargetType">The type of TrackerTarget.</param>
    /// <returns>True if vibration is enabled. False if vibration is disabled.</returns>
    public bool IsTrackerTargetVibrationEnabled(TrackerTargetType trackerTargetType)
    {
        bool isVibrationEnabled = false;
        zsupIsTargetVibrationEnabled((int)trackerTargetType, out isVibrationEnabled);
        return isVibrationEnabled;
    }

    /// <summary>
    /// Check whether or not the TrackerTarget is currently vibrating.
    /// </summary>
    /// <param name="trackerTargetType">The type of TrackerTarget.</param>
    /// <returns>True if vibrating. False if not.</returns>
    public bool IsTrackerTargetVibrating(TrackerTargetType trackerTargetType)
    {
        bool isVibrating = false;
        zsupIsTargetVibrating((int)trackerTargetType, out isVibrating);
        return isVibrating;
    }

    /// <summary>
    /// Start vibrating the TrackerTarget based on a specified "on period", "off period",
    /// and number of times.
    /// </summary>
    /// <param name="trackerTargetType">The type of TrackerTarget.</param>
    /// 
    /// <param name="onPeriod">
    /// The duration in seconds of the vibration.
    /// </param>
    /// 
    /// <param name="offPeriod">
    /// The duration in seconds between vibrations.
    /// </param>
    /// 
    /// <param name="numTimes">
    /// The number of times the vibration occurs:
    /// -1 -> Vibrate infinitely
    ///  0 -> Do nothing
    ///  N -> Vibrate N times
    /// </param>
    public void StartTrackerTargetVibration(TrackerTargetType trackerTargetType, float onPeriod, float offPeriod, int numTimes)
    {
        zsupStartTargetVibration((int)trackerTargetType, onPeriod, offPeriod, numTimes);
    }

    /// <summary>
    /// Stop vibrating the TrackerTarget if it is currently vibrating.  If StartTrackerTargetVibration() is
    /// called again, the TrackerTarget will start vibrating the full sequence of "on" and "off" cycles.
    /// </summary>
    public void StopTrackerTargetVibration(TrackerTargetType trackerTargetType)
    {
        zsupStopTargetVibration((int)trackerTargetType);
    }

    /// <summary>
    /// Check whether or not the TrackerTarget is tapping the display.
    /// </summary>
    /// <param name="trackerTargetType">The type of TrackerTarget.</param>
    /// <returns>Whether or not the TrackerTarget is currently tapping the display.</returns>
    public bool IsTrackerTargetTapPressed(TrackerTargetType trackerTargetType)
    {
        bool isTapPressed = false;
        zsupIsTargetTapPressed((int)trackerTargetType, out isTapPressed);
        return isTapPressed;
    }

    /// <summary>
    /// Set whether or no mouse emulation is enabled.
    /// </summary>
    /// <param name="isMouseEmulationEnabled">True to enable mouse emulation, false otherwise.</param>
    public void SetMouseEmulationEnabled(bool isMouseEmulationEnabled)
    {
        this.EnableMouseEmulation = isMouseEmulationEnabled;
        zsupSetMouseEmulationEnabled(isMouseEmulationEnabled);
    }

    /// <summary>
    /// Check whether or not mouse emulation is enabled.
    /// </summary>
    /// <returns>True if mouse emulation is enabled.  False if not.</returns>
    public bool IsMouseEmulationEnabled()
    {
        bool isEnabled = false;
        zsupIsMouseEmulationEnabled(out isEnabled);
        return isEnabled;
    }

    /// <summary>
    /// Specify the TrackerTarget that will emulate the mouse.
    /// </summary>
    /// <param name="trackerTargetType">The type of TrackerTarget.</param>
    public void SetMouseEmulationTarget(TrackerTargetType trackerTargetType)
    {
        zsupSetMouseEmulationTarget((int)trackerTargetType);
    }

    /// <summary>
    /// Set the movement mode for mouse emulation.
    /// </summary>
    /// <param name="movementMode"></param>
    public void SetMouseEmulationMovementMode(MouseMovementMode movementMode)
    {
        zsupSetMouseEmulationMovementMode((int)movementMode);
    }

    /// <summary>
    /// Get the movement mode of mouse emulation.
    /// </summary>
    /// <returns></returns>
    public MouseMovementMode GetMouseEmulationMovementMode()
    {
        int movementMode = 0;
        zsupGetMouseEmulationMovementMode(out movementMode);
        return (MouseMovementMode)movementMode;
    }

    /// <summary>
    /// Set the distance at which mouse emulation will be enabled.
    /// </summary>
    /// <param name="mouseEmulationDistance">The mouse emulation distance.</param>
    public void SetMouseEmulationDistance(float mouseEmulationDistance)
    {
        zsupSetMouseEmulationMaxDistance(mouseEmulationDistance);
    }

    /// <summary>
    /// Get the distance at which mouse emulation will be enabled.
    /// </summary>
    /// <returns>The mouse emulation distance.</returns>
    public float GetMouseEmulationDistance()
    {
        float maxDistance = 0.0f;
        zsupGetMouseEmulationMaxDistance(out maxDistance);
        return maxDistance;
    }

    /// <summary>
    /// Map a specified TrackerTarget button to a mouse button.
    /// </summary>
    /// <param name="buttonId">TrackerTarget button id.</param>
    /// <param name="mouseButton">Mouse button.</param>
    public void SetMouseEmulationButtonMapping(int buttonId, MouseButton mouseButton)
    {
        zsupSetMouseEmulationButtonMapping(buttonId, (int)mouseButton);
    }

    /// <summary>
    /// Get the mouse button that the specified button id is mapped to.
    /// </summary>
    /// <param name="buttonId">TrackerTarget button id.</param>
    /// <returns>Mouse button.</returns>
    public MouseButton GetMouseEmulationButtonMapping(int buttonId)
    {
        int mouseButton = 0;
        zsupGetMouseEmulationButtonMapping(buttonId, out mouseButton);
        return (MouseButton)mouseButton;
    }

    /// <summary>
    /// Get a camera from the ZSCore stereo rig based on a
    /// specified camera type.
    /// </summary>
    /// <param name="cameraType">The camera type: Left, Right, or Final</param>
    /// <returns>Reference to the underlying Unity camera</returns>
    public Camera GetStereoCamera(CameraType cameraType)
    {
        return _stereoCameras[(int)cameraType];
    }

    /// <summary>
    /// Convert a matrix in right handed space to left handed space.
    /// </summary>
    /// <param name="rightHandMatrix">A right handed matrix.</param>
    /// <returns>A left handed matrix.</returns>
    public static Matrix4x4 ConvertFromRightToLeft(Matrix4x4 right)
    {
        return RIGHT_TO_LEFT * right * RIGHT_TO_LEFT;
    }

    #endregion


    #region EVENTS

    public delegate void CoreEventHandler(ZSCore sender);
    public event CoreEventHandler Updated;

    protected void RaiseUpdated()
    {
        if (Updated != null)
            Updated(this);
    }

    #endregion


    #region PRIVATE HELPERS

    /// <summary>
    /// Check whether or not the CurrentCamera is valid.
    /// </summary>
    private bool IsCurrentCameraValid()
    {
        return (this.CurrentCamera != null && this.CurrentCamera.camera != null);
    }


    private void Initialize()
    {
        // Initialize the cached stereo information.
        for (int i = 0; i < (int)Eye.NumEyes; ++i)
        {
            _viewMatrices[i]        = Matrix4x4.identity;
            _projectionMatrices[i]  = Matrix4x4.identity;
        }

        // Initialize the cached tracker information.
        for (int i = 0; i < (int)TrackerTargetType.NumTypes; ++i)
        {
            _isTrackerTargetVisible[i] = false;
            _trackerTargetPoses[i]       = Matrix4x4.identity;
            _trackerTargetCameraPoses[i] = Matrix4x4.identity;
            _trackerTargetWorldPoses[i]  = Matrix4x4.identity;
        }
    }

    /// <summary>
    /// Initialize the left and right stereo cameras.
    /// </summary>
    private void InitializeStereoCameras()
    {
        _stereoCameras[(int)CameraType.Left]  = GameObject.Find("ZSLeftCamera").camera;
        _stereoCameras[(int)CameraType.Right] = GameObject.Find("ZSRightCamera").camera;
        _stereoCameras[(int)CameraType.Final] = GameObject.Find("ZSFinalCamera").camera;

        _stereoCameras[(int)CameraType.Left].enabled  = false;
        _stereoCameras[(int)CameraType.Right].enabled = false;
        _stereoCameras[(int)CameraType.Final].enabled = false;

        this.CheckCurrentCameraChanged();
    }

    /// <summary>
    /// Copy a certain subset of camera attributes from a 
    /// source camera to a destination camera.
    /// </summary>
    private void CopyCameraAttributes(Camera source, ref Camera destination)
    {
        if (source != null && destination != null)
        {
            destination.clearFlags      = source.clearFlags;
            destination.backgroundColor = source.backgroundColor;
            destination.cullingMask     = source.cullingMask;
        }
    }

    /// <summary>
    /// Check to see if the current camera has changed.
    /// </summary>
    private void CheckCurrentCameraChanged()
    {
        if (_previousCamera != this.CurrentCamera)
        {
            float currentCameraDepth = 0.0f;

            if (this.IsCurrentCameraValid())
            {
                Camera currentCamera = this.CurrentCamera.camera;

                // Grab the current camera depth.
                currentCameraDepth = currentCamera.depth;
        
                // Set the near/far clip planes.
                this.SetNearClip(currentCamera.nearClipPlane);
                this.SetFarClip(currentCamera.farClipPlane);

                // Copy a subset of camera attributes from the
                // CurrentCamera to the Left/Right cameras.
                this.CopyCameraAttributes(currentCamera, ref _stereoCameras[(int)CameraType.Left]);
                this.CopyCameraAttributes(currentCamera, ref _stereoCameras[(int)CameraType.Right]);
            }

            // Set the Left, Right, and Final Camera depth values.
            if (_stereoCameras[(int)CameraType.Left] != null)
                _stereoCameras[(int)CameraType.Left].depth = currentCameraDepth + 1.0f;

            if (_stereoCameras[(int)CameraType.Right] != null)
                _stereoCameras[(int)CameraType.Right].depth = currentCameraDepth + 2.0f;

            if (_stereoCameras[(int)CameraType.Final] != null)
                _stereoCameras[(int)CameraType.Final].depth = currentCameraDepth + 3.0f;

            _previousCamera = this.CurrentCamera;
        }
    }

    /// <summary>
    /// Check for any updates to public properties.
    /// </summary>
    private void CheckForUpdates()
    {
        if (this.EnableStereo != this.IsStereoEnabled())
            this.SetStereoEnabled(this.EnableStereo);

        if (this.EnableTracking != this.IsTrackingEnabled())
            this.SetTrackingEnabled(this.EnableTracking);

        if (this.EnableMouseEmulation != this.IsMouseEmulationEnabled())
            this.SetMouseEmulationEnabled(this.EnableMouseEmulation);

        if (this.InterPupillaryDistance != this.GetInterPupillaryDistance())
            this.SetInterPupillaryDistance(this.InterPupillaryDistance);

        if (this.ViewerScale != this.GetViewerScale())
            this.SetViewerScale(this.ViewerScale);

        if (this.FieldOfViewScale != this.GetFieldOfViewScale())
            this.SetFieldOfViewScale(this.FieldOfViewScale);

        if (this.HeadTrackingScale != this.GetHeadTrackingScale())
            this.SetHeadTrackingScale(this.HeadTrackingScale);

        if (this.GlassesOffset != this.GetGlassesOffset())
            this.SetGlassesOffset(this.GlassesOffset);
    }

    /// <summary>
    /// Update all of the stereo and tracker information.
    /// </summary>
    private void UpdateInternal()
    {
        if (_coreSingleton.IsInitialized)
        {
            this.CheckCurrentCameraChanged();
            this.CheckForUpdates();

            // Perform an update on the TrackerTargets and StereoFrustum.
            zsupUpdate();

            this.UpdateLRDetectInternal();
            this.UpdateStereoInternal();
            this.UpdateTrackerInternal();

            // Set the final camera to be enabled so that it can reset the draw buffer
            // to the back buffer for the next frame.
            if (this.IsStereoEnabled() && _stereoCameras[(int)CameraType.Final] != null)
                _stereoCameras[(int)CameraType.Final].enabled = true;

            // Set the current camera to be enabled so that Camera.main does not return null
            // when referenced in Awake, Start, Update, etc. 
            if (this.IsStereoEnabled() && this.IsCurrentCameraValid())
                this.CurrentCamera.camera.enabled = true;

            // Raise the Updated event.
            this.RaiseUpdated();

            // Cache previous state.
            _wasFullScreen = Screen.fullScreen;
        }
    }

    /// <summary>
    /// Update L/R Detect.
    /// </summary>
    private void UpdateLRDetectInternal()
    {
        // Update L/R Detect.
        if (Screen.fullScreen)
            GL.IssuePluginEvent((int)ZSCore.GlPluginEventType.UpdateLRDetectFullscreen);
        else
            GL.IssuePluginEvent((int)ZSCore.GlPluginEventType.UpdateLRDetectWindowed);

        // For ATI cards, transitioning from windowed mode to fullscreen mode
        // can sometimes cause the left/right frames to become out of sync.
        // Force a sync for this case.
        if (SystemInfo.graphicsDeviceVendorID == 4098)
        {
            if (_wasFullScreen == false && Screen.fullScreen == true)
                GL.IssuePluginEvent((int)ZSCore.GlPluginEventType.SyncLRDetectFullscreen);
        }
    }

    /// <summary>
    /// Update all of the stereo information.
    /// </summary>
    private void UpdateStereoInternal()
    {
        // Update the window dimensions if they have changed.
        zsupSetViewportSize(Screen.width, Screen.height);

        // Get the view and projection matrices.
        for (int i = 0; i < (int)Eye.NumEyes; ++i)
        {
            zsupGetFrustumViewMatrix(i, _matrixData);
            _viewMatrices[i] = this.ConvertToMatrix4x4(_matrixData);

            zsupGetFrustumProjectionMatrix(i, _matrixData);
            _projectionMatrices[i] = this.ConvertToMatrix4x4(_matrixData);
        }
    }

    /// <summary>
    /// Update all of the tracker information.
    /// </summary>
    private void UpdateTrackerInternal()
    {
        double timestamp = 0.0;

        // Get the tracker, camera, and world space target poses.
        for (int i = 0; i < (int)TrackerTargetType.NumTypes; ++i)
        {
            // Get whether or not pose is valid.
            zsupIsTargetVisible(i, out _isTrackerTargetVisible[i]);
            
            if (_isTrackerTargetVisible[i])
            {
                // Tracker space poses.
                zsupGetTargetPose(i, _matrixData, out timestamp);
                _trackerTargetPoses[i] = this.ConvertToMatrix4x4(_matrixData);

                // Camera space poses.
                zsupGetTargetCameraPose(i, _matrixData, out timestamp);
                _trackerTargetCameraPoses[i] = ZSCore.ConvertFromRightToLeft(this.ConvertToMatrix4x4(_matrixData));

                // World space poses.
                _trackerTargetWorldPoses[i] = _trackerTargetCameraPoses[i];

                // Scale the position based on world and field of view scales.
                _trackerTargetWorldPoses[i][0, 3] *= this.FieldOfViewScale;
                _trackerTargetWorldPoses[i][1, 3] *= this.FieldOfViewScale;
              
                // Convert the camera space pose to world space.
                if (this.IsCurrentCameraValid())
                    _trackerTargetWorldPoses[i] = this.CurrentCamera.transform.localToWorldMatrix * _trackerTargetWorldPoses[i];
            }
        }
    }

    /// <summary>
    /// Convert an array of 16 floats to Unity's Matrix4x4 format.
    /// </summary>
    /// <param name="matrixData">The matrix data stored in a float array.</param>
    /// <returns>The matrix data in Matrix4x4 format.</returns>
    private Matrix4x4 ConvertToMatrix4x4(float[/*16*/] matrixData)
    {
        Matrix4x4 matrix = new Matrix4x4();

        for (int i = 0; i < 16; i++)
            matrix[i] = matrixData[i];

        return matrix;
    }

    /// <summary>
    /// Convert an array of 2 floats to Unity's Vector2 format.
    /// </summary>
    /// <param name="vectorData">The vector data stored in a float array.</param>
    /// <returns>The vector data in Vector2 format.</returns>
    private Vector2 ConvertToVector2(float[/*2*/] vectorData)
    {
        return new Vector2(vectorData[0], vectorData[1]);
    }

    /// <summary>
    /// Convert an array of 3 floats to Unity's Vector3 format.
    /// </summary>
    /// <param name="vectorData">The vector data stored in a float array.</param>
    /// <returns>The vector data in Vector3 format.</returns>
    private Vector3 ConvertToVector3(float[/*3*/] vectorData)
    {
        return new Vector3(vectorData[0], vectorData[1], vectorData[2]);
    }

    /// <summary>
    /// Convert a Vector3 to a float array.
    /// </summary>
    /// <param name="vector3">Vector3 data in Unity's Vector3 format.</param>
    /// <returns>Vector data in float array format.</returns>
    private float[] ConvertToFloatArray(Vector3 vector3)
    {
        float[] array = new float[3];
        array[0] = vector3.x;
        array[1] = vector3.y;
        array[2] = vector3.z;

        return array;
    }

    /// <summary>
    /// Convert a Matrix4x4 to a float array.
    /// </summary>
    /// <param name="matrix">Matrix data in Unity's Matrix4x4 format.</param>
    /// <returns>Matrix data in float array format.</returns>
    private float[] ConvertToFloatArray(Matrix4x4 matrix)
    {
        float[] array = new float[16];

        for (int i = 0; i < 16; i++)
            array[i] = matrix[i];

        return array;
    }

    /// <summary>
    /// The update coroutine.
    /// This will continue after the end of the frame has been hit.
    /// </summary>
    IEnumerator UpdateCoroutine()
    {
        while (true)
        {
            // Perform an update.
            this.UpdateInternal();

            // Wait for the end of the frame.
            yield return new WaitForEndOfFrame();
        }
    }

    #endregion


    #region PRIVATE MEMBERS

    // Constants
    private readonly static Matrix4x4 RIGHT_TO_LEFT = Matrix4x4.Scale(new Vector4(1.0f, 1.0f, -1.0f));

    private readonly static int[] BLACK     = { 0, 0, 0 };
    private readonly static int[] WHITE     = { 1, 1, 1 };
    private readonly static int[] RED       = { 1, 0, 0 };
    private readonly static int[] GREEN     = { 0, 1, 0 };
    private readonly static int[] BLUE      = { 0, 0, 1 };
    private readonly static int[] CYAN      = { 0, 1, 1 };
    private readonly static int[] MAGENTA   = { 1, 0, 1 };
    private readonly static int[] YELLOW    = { 1, 1, 0 };


    // Non-Constants
    private ZSCoreSingleton _coreSingleton   = null;

    private bool            _isStereoEnabled = false;
    private bool            _areEyesSwapped  = false;
    private bool            _wasFullScreen   = false;

    private float[]         _matrixData      = new float[16];

    private Matrix4x4[]     _viewMatrices       = new Matrix4x4[(int)Eye.NumEyes];
    private Matrix4x4[]     _projectionMatrices = new Matrix4x4[(int)Eye.NumEyes];

    private bool[]          _isTrackerTargetVisible   = new bool[(int)TrackerTargetType.NumTypes];
    private Matrix4x4[]     _trackerTargetPoses       = new Matrix4x4[(int)TrackerTargetType.NumTypes];
    private Matrix4x4[]     _trackerTargetCameraPoses = new Matrix4x4[(int)TrackerTargetType.NumTypes];
    private Matrix4x4[]     _trackerTargetWorldPoses  = new Matrix4x4[(int)TrackerTargetType.NumTypes];

    private GameObject      _previousCamera  = null;
    private Camera[]        _stereoCameras   = new Camera[(int)CameraType.NumTypes];
    
    private List<int[]>     _ledColors = new List<int[]>() { BLACK, WHITE, RED, GREEN, BLUE, CYAN, MAGENTA, YELLOW };

    #endregion


    #region ZSPACE PLUGIN IMPORT DECLARATIONS

    [DllImport("zSpaceUnity")]
    private static extern bool zsupIsGraphicsDeviceInitialized();
    [DllImport("zSpaceUnity")]
    private static extern void zsupSetRunningInEditor(bool isRunningInEditor);

    // General API
    [DllImport("zSpaceUnity")]
    private static extern int  zsupInitialize();
    [DllImport("zSpaceUnity")]
    private static extern int  zsupUpdate();
    [DllImport("zSpaceUnity")]
    private static extern int  zsupShutdown();
    [DllImport("zSpaceUnity")]
    private static extern int  zsupSetTrackingEnabled(bool isEnabled);
    [DllImport("zSpaceUnity")]
    private static extern int  zsupIsTrackingEnabled(out bool isEnabled);

    // Display API
    [DllImport("zSpaceUnity")]
    private static extern int  zsupGetDisplayPosition(out int x, out int y);
    [DllImport("zSpaceUnity")]
    private static extern int  zsupGetDisplaySize(out float width, out float height);
    [DllImport("zSpaceUnity")]
    private static extern int  zsupGetDisplayResolution(out int x, out int y);
    [DllImport("zSpaceUnity")]
    private static extern int  zsupGetDisplayAngle(out float x, out float y, out float z);
    [DllImport("zSpaceUnity")]
    private static extern int  zsupIsDisplayHardwarePresent(out bool isHardwarePresent);
    [DllImport("zSpaceUnity")]
    private static extern int  zsupIntersectDisplay(float[/*16*/] poseMatrix, out bool hit, out int x, out int y, out int nx, out int ny, out float distance);

    // StereoViewport API
    [DllImport("zSpaceUnity")]
    private static extern int  zsupSetViewportPosition(int x, int y);
    [DllImport("zSpaceUnity")]
    private static extern int  zsupGetViewportPosition(out int x, out int y);
    [DllImport("zSpaceUnity")]
    private static extern int  zsupSetViewportSize(int width, int height);
    [DllImport("zSpaceUnity")]
    private static extern int  zsupGetViewportSize(out int width, out int height);

    // Coordinate Space API
    [DllImport("zSpaceUnity")]
    private static extern int  zsupGetCoordinateSpaceTransform(int a, int b, float[/*16*/] transform);

    // StereoFrustum API
    [DllImport("zSpaceUnity")]
    private static extern int  zsupSetFrustumAttribute(int attribute, float value);
    [DllImport("zSpaceUnity")]
    private static extern int  zsupGetFrustumAttribute(int attribute, out float value);
    [DllImport("zSpaceUnity")]
    private static extern int  zsupSetFrustumPortalMode(int portalModeFlags);
    [DllImport("zSpaceUnity")]
    private static extern int  zsupGetFrustumPortalMode(out int portalModeFlags);
    [DllImport("zSpaceUnity")]
    private static extern int  zsupSetFrustumCameraOffset(float[/*3*/] cameraOffset);
    [DllImport("zSpaceUnity")]
    private static extern int  zsupGetFrustumCameraOffset(float[/*3*/] cameraOffset);
    [DllImport("zSpaceUnity")]
    private static extern int  zsupGetFrustumViewMatrix(int eye, float[/*16*/] viewMatrix);
    [DllImport("zSpaceUnity")]
    private static extern int  zsupGetFrustumProjectionMatrix(int eye, float[/*16*/] projectionMatrix);
    [DllImport("zSpaceUnity")]
    private static extern int  zsupGetFrustumEyePosition(int eye, float[/*3*/] eyePosition);
    [DllImport("zSpaceUnity")]
    private static extern int  zsupGetFrustumBounds(int eye, float[/*6*/] frustumBounds);

    // TrackerTarget API
    [DllImport("zSpaceUnity")]
    private static extern int  zsupSetTargetEnabled(int targetType, bool isEnabled);
    [DllImport("zSpaceUnity")]
    private static extern int  zsupIsTargetEnabled(int targetType, out bool isEnabled);
    [DllImport("zSpaceUnity")]
    private static extern int  zsupIsTargetVisible(int targetType, out bool isVisible);
    [DllImport("zSpaceUnity")]
    private static extern int  zsupGetTargetPose(int targetType, float[/*16*/] poseMatrix, out double timestamp);
    [DllImport("zSpaceUnity")]
    private static extern int  zsupGetTargetCameraPose(int targetType, float[/*16*/] poseMatrix, out double timestamp);
    [DllImport("zSpaceUnity")]
    private static extern int  zsupSetTargetPoseBufferingEnabled(int targetType, bool isEnabled);
    [DllImport("zSpaceUnity")]
    private static extern int  zsupIsTargetPoseBufferingEnabled(int targetType, out bool isEnabled);
    [DllImport("zSpaceUnity")]
    private static extern int  zsupGetTargetBufferedPose(int targetType, float seconds, float[/*16*/] poseMatrix, out double timestamp);
    [DllImport("zSpaceUnity")]
    private static extern int  zsupGetTargetBufferedCameraPose(int targetType, float seconds, float[/*16*/] poseMatrix, out double timestamp);

    // TrackerTarget Button API
    [DllImport("zSpaceUnity")]
    private static extern int  zsupGetNumTargetButtons(int targetType, out int numButtons);
    [DllImport("zSpaceUnity")]
    private static extern int  zsupIsTargetButtonPressed(int targetType, int buttonId, out bool isPressed);

    // TrackerTarget Led API
    [DllImport("zSpaceUnity")]
    private static extern int  zsupSetTargetLedEnabled(int targetType, bool isEnabled);
    [DllImport("zSpaceUnity")]
    private static extern int  zsupIsTargetLedEnabled(int targetType, out bool isEnabled);
    [DllImport("zSpaceUnity")]
    private static extern int  zsupIsTargetLedOn(int targetType, out bool isOn);
    [DllImport("zSpaceUnity")]
    private static extern int  zsupSetTargetLedColor(int targetType, float r, float g, float b);
    [DllImport("zSpaceUnity")]
    private static extern int  zsupGetTargetLedColor(int targetType, out float r, out float g, out float b);

    // TrackerTarget Vibration API
    [DllImport("zSpaceUnity")]
    private static extern int  zsupSetTargetVibrationEnabled(int targetType, bool isEnabled);
    [DllImport("zSpaceUnity")]
    private static extern int  zsupIsTargetVibrationEnabled(int targetType, out bool isEnabled);
    [DllImport("zSpaceUnity")]
    private static extern int  zsupIsTargetVibrating(int targetType, out bool isVibrating);
    [DllImport("zSpaceUnity")]
    private static extern int  zsupStartTargetVibration(int targetType, float onPeriod, float offPeriod, int numTimes);
    [DllImport("zSpaceUnity")]
    private static extern int  zsupStopTargetVibration(int targetType);
    
    // TrackerTarget Tap API
    [DllImport("zSpaceUnity")]
    private static extern int  zsupIsTargetTapPressed(int targetType, out bool isPressed);
    [DllImport("zSpaceUnity")]
    private static extern int  zsupSetTargetTapHoldThreshold(int targetType, float seconds);
    [DllImport("zSpaceUnity")]
    private static extern int  zsupGetTargetTapHoldThreshold(int targetType, out float seconds);

    // Mouse Emulation API
    [DllImport("zSpaceUnity")]
    private static extern int  zsupSetMouseEmulationEnabled(bool isEnabled);
    [DllImport("zSpaceUnity")]
    private static extern int  zsupIsMouseEmulationEnabled(out bool isEnabled);
    [DllImport("zSpaceUnity")]
    private static extern int  zsupSetMouseEmulationTarget(int targetType);
    [DllImport("zSpaceUnity")]
    private static extern int  zsupGetMouseEmulationTarget(out int targetType);
    [DllImport("zSpaceUnity")]
    private static extern int  zsupSetMouseEmulationMovementMode(int movementMode);
    [DllImport("zSpaceUnity")]
    private static extern int  zsupGetMouseEmulationMovementMode(out int movementMode);
    [DllImport("zSpaceUnity")]
    private static extern int  zsupSetMouseEmulationMaxDistance(float maxDistance);
    [DllImport("zSpaceUnity")]
    private static extern int  zsupGetMouseEmulationMaxDistance(out float maxDistance);
    [DllImport("zSpaceUnity")]
    private static extern int  zsupSetMouseEmulationButtonMapping(int buttonId, int mouseButton);
    [DllImport("zSpaceUnity")]
    private static extern int  zsupGetMouseEmulationButtonMapping(int buttonId, out int mouseButton);

    #endregion
}
