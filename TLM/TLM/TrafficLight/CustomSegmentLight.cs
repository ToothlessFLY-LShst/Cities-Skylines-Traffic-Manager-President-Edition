﻿#define DEBUGVISUALSx

using System;
using System.Collections.Generic;
using ColossalFramework;
using TrafficManager.Geometry;
using UnityEngine;
using TrafficManager.Custom.AI;

namespace TrafficManager.TrafficLight {
	/// <summary>
	/// Represents the traffic light (left, forward, right) at a specific segment end
	/// </summary>
	public class CustomSegmentLight : ICloneable {
		public enum Mode {
			Simple = 1, // <^>
			SingleLeft = 2, // <, ^>
			SingleRight = 3, // <^, >
			All = 4 // <, ^, >
		}

		public ushort NodeId {
			get; private set;
		}

		public ushort SegmentId {
			get; set;
		}

		public Mode CurrentMode {
			get; set;
		} = Mode.Simple;

		private RoadBaseAI.TrafficLightState leftLight;
		private RoadBaseAI.TrafficLightState mainLight;
		private RoadBaseAI.TrafficLightState rightLight;

		public RoadBaseAI.TrafficLightState LightLeft {
			get { return leftLight; }
			set {
				if (leftLight != value)
					lights.OnChange();
				leftLight = value;
			}
		}

		public RoadBaseAI.TrafficLightState LightMain {
			get { return mainLight; }
			set {
				if (mainLight != value)
					lights.OnChange();
				mainLight = value;
			}
		}
		public RoadBaseAI.TrafficLightState LightRight {
			get { return rightLight; }
			set {
				if (rightLight != value)
					lights.OnChange();
				rightLight = value;
			}
		}

		CustomSegmentLights lights;

		public override string ToString() {
			return $"LightLeft={LightLeft} LightMain={LightMain} LightRight={LightRight} CurrentMode={CurrentMode}";
		}

		public CustomSegmentLight(CustomSegmentLights lights, ushort nodeId, ushort segmentId, RoadBaseAI.TrafficLightState mainLight) {
			this.NodeId = nodeId;
			this.SegmentId = segmentId;
			this.lights = lights;

			LightMain = mainLight;
			LightLeft = mainLight;
			LightRight = mainLight;

			UpdateVisuals();
		}

		public CustomSegmentLight(CustomSegmentLights lights, ushort nodeId, ushort segmentId, RoadBaseAI.TrafficLightState mainLight, RoadBaseAI.TrafficLightState leftLight, RoadBaseAI.TrafficLightState rightLight/*, RoadBaseAI.TrafficLightState pedestrianLight*/) {
			this.NodeId = nodeId;
			this.SegmentId = segmentId;
			this.lights = lights;

			LightMain = mainLight;
			LightLeft = leftLight;
			LightRight = rightLight;

			UpdateVisuals();
		}

		public RoadBaseAI.TrafficLightState GetLightMain() {
			return LightMain;
		}

		public RoadBaseAI.TrafficLightState GetLightLeft() {
			return LightLeft;
		}

		public RoadBaseAI.TrafficLightState GetLightRight() {
			return LightRight;
		}

		public void ChangeMode() {
			SegmentGeometry geometry = SegmentGeometry.Get(SegmentId);
			//geometry.Recalculate(true, true);
			bool startNode = geometry.StartNodeId() == NodeId;
			var hasLeftSegment = geometry.HasOutgoingLeftSegment(startNode);
			var hasForwardSegment = geometry.HasOutgoingStraightSegment(startNode);
			var hasRightSegment = geometry.HasOutgoingRightSegment(startNode);

#if DEBUG
			Log._Debug($"ChangeMode. segment {SegmentId} @ node {NodeId}, hasOutgoingLeft={hasLeftSegment}, hasOutgoingStraight={hasForwardSegment}, hasOutgoingRight={hasRightSegment}");
#endif

			if (CurrentMode == Mode.Simple) {
				if (!hasLeftSegment) {
					CurrentMode = Mode.SingleRight;
				} else {
					CurrentMode = Mode.SingleLeft;
				}
			} else if (CurrentMode == Mode.SingleLeft) {
				if (!hasForwardSegment || !hasRightSegment) {
					CurrentMode = Mode.Simple;
				} else {
					CurrentMode = Mode.SingleRight;
				}
			} else if (CurrentMode == Mode.SingleRight) {
				if (!hasLeftSegment) {
					CurrentMode = Mode.Simple;
				} else {
					CurrentMode = Mode.All;
				}
			} else {
				CurrentMode = Mode.Simple;
			}

			switch (CurrentMode) {
				case Mode.Simple:
					leftLight = mainLight;
					rightLight = mainLight;
					break;
				case Mode.SingleLeft:
					rightLight = mainLight;
					break;
				case Mode.SingleRight:
					leftLight = mainLight;
					break;
			}
		}

		public void ChangeLightMain() {
			var invertedLight = LightMain == RoadBaseAI.TrafficLightState.Green
				? RoadBaseAI.TrafficLightState.Red
				: RoadBaseAI.TrafficLightState.Green;

			if (CurrentMode == Mode.Simple) {
				LightLeft = invertedLight;
				LightRight = invertedLight;
				//LightPedestrian = !PedestrianEnabled ? LightMain : LightPedestrian;
				LightMain = invertedLight;
			} else if (CurrentMode == Mode.SingleLeft) {
				LightRight = invertedLight;
				LightMain = invertedLight;
			} else if (CurrentMode == Mode.SingleRight) {
				LightLeft = invertedLight;
				LightMain = invertedLight;
			} else {
				LightMain = invertedLight;
			}

			UpdateVisuals();
		}

		public void ChangeLightLeft() {
			var invertedLight = LightLeft == RoadBaseAI.TrafficLightState.Green
				? RoadBaseAI.TrafficLightState.Red
				: RoadBaseAI.TrafficLightState.Green;

			LightLeft = invertedLight;

			UpdateVisuals();
		}

		public void ChangeLightRight() {
			var invertedLight = LightRight == RoadBaseAI.TrafficLightState.Green
				? RoadBaseAI.TrafficLightState.Red
				: RoadBaseAI.TrafficLightState.Green;

			LightRight = invertedLight;

			UpdateVisuals();
		}

		public bool isAnyGreen() {
			return LightMain == RoadBaseAI.TrafficLightState.Green ||
				LightLeft == RoadBaseAI.TrafficLightState.Green ||
				LightRight == RoadBaseAI.TrafficLightState.Green;
		}

		public bool isAnyInTransition() {
			return LightMain == RoadBaseAI.TrafficLightState.RedToGreen ||
				LightLeft == RoadBaseAI.TrafficLightState.RedToGreen ||
				LightRight == RoadBaseAI.TrafficLightState.RedToGreen ||
				LightMain == RoadBaseAI.TrafficLightState.GreenToRed ||
				LightLeft == RoadBaseAI.TrafficLightState.GreenToRed ||
				LightRight == RoadBaseAI.TrafficLightState.GreenToRed;
		}

		public bool isLeftGreen() {
			return LightLeft == RoadBaseAI.TrafficLightState.Green;
		}

		public bool isForwardGreen() {
			return LightMain == RoadBaseAI.TrafficLightState.Green;
		}

		public bool isRightGreen() {
			return LightRight == RoadBaseAI.TrafficLightState.Green;
		}

		public void UpdateVisuals() {
#if TRACE
			Singleton<CodeProfiler>.instance.Start("CustomSegmentLight.UpdateVisuals");
#endif
			var instance = Singleton<NetManager>.instance;

			uint currentFrameIndex = Singleton<SimulationManager>.instance.m_currentFrameIndex;
			uint num = (uint)(((int)NodeId << 8) / 32768);

			RoadBaseAI.TrafficLightState vehicleLightState;
			RoadBaseAI.TrafficLightState pedestrianLightState;

			RoadBaseAI.TrafficLightState mainLight = LightMain;
			RoadBaseAI.TrafficLightState leftLight = LightLeft;
			RoadBaseAI.TrafficLightState rightLight = LightRight;

			switch (CurrentMode) {
				case Mode.Simple:
					leftLight = mainLight;
					rightLight = mainLight;
					break;
				case Mode.SingleLeft:
					rightLight = mainLight;
					break;
				case Mode.SingleRight:
					leftLight = mainLight;
					break;
				case Mode.All:
				default:
					break;
			}

			vehicleLightState = GetVisualLightState();
			pedestrianLightState = lights.PedestrianLightState == null ? RoadBaseAI.TrafficLightState.Red : (RoadBaseAI.TrafficLightState)lights.PedestrianLightState;

#if DEBUGVISUALS
			Log._Debug($"Setting visual traffic light state of node {NodeId}, seg. {SegmentId} to vehicleState={vehicleLightState} pedState={pedestrianLightState}");
#endif

			uint now = ((currentFrameIndex - num) >> 8) & 1;
			CustomRoadAI.OriginalSetTrafficLightState(true, NodeId, ref instance.m_segments.m_buffer[SegmentId], now << 8, vehicleLightState, pedestrianLightState, false, false);
			CustomRoadAI.OriginalSetTrafficLightState(true, NodeId, ref instance.m_segments.m_buffer[SegmentId], (1u - now) << 8, vehicleLightState, pedestrianLightState, false, false);
#if TRACE
			Singleton<CodeProfiler>.instance.Stop("CustomSegmentLight.UpdateVisuals");
#endif
		}

		public RoadBaseAI.TrafficLightState GetVisualLightState() {
#if TRACE
			Singleton<CodeProfiler>.instance.Start("CustomSegmentLight.GetVisualLightState");
#endif
			RoadBaseAI.TrafficLightState vehicleLightState;
			// any green?
			if (mainLight == RoadBaseAI.TrafficLightState.Green ||
				leftLight == RoadBaseAI.TrafficLightState.Green ||
				rightLight == RoadBaseAI.TrafficLightState.Green) {
				vehicleLightState = RoadBaseAI.TrafficLightState.Green;
			} else // all red?
			if (mainLight == RoadBaseAI.TrafficLightState.Red &&
				leftLight == RoadBaseAI.TrafficLightState.Red &&
				rightLight == RoadBaseAI.TrafficLightState.Red) {
				vehicleLightState = RoadBaseAI.TrafficLightState.Red;
			} else // any red+yellow?
			if (mainLight == RoadBaseAI.TrafficLightState.RedToGreen ||
				leftLight == RoadBaseAI.TrafficLightState.RedToGreen ||
				rightLight == RoadBaseAI.TrafficLightState.RedToGreen) {
				vehicleLightState = RoadBaseAI.TrafficLightState.RedToGreen;
			} else {
				vehicleLightState = RoadBaseAI.TrafficLightState.GreenToRed;
			}

#if TRACE
			Singleton<CodeProfiler>.instance.Stop("CustomSegmentLight.GetVisualLightState");
#endif
			return vehicleLightState;
		}

		private RoadBaseAI.TrafficLightState _checkPedestrianLight() {
			if (LightLeft == RoadBaseAI.TrafficLightState.Red && LightMain == RoadBaseAI.TrafficLightState.Red &&
				LightRight == RoadBaseAI.TrafficLightState.Red) {
				return RoadBaseAI.TrafficLightState.Green;
			}
			return RoadBaseAI.TrafficLightState.Red;
		}

		public object Clone() {
			return MemberwiseClone();
		}

		/*public void invert() {
			LightMain = InvertLight(LightMain);
			LightLeft = InvertLight(LightLeft);
			LightRight = InvertLight(LightRight);
		}*/

		public static RoadBaseAI.TrafficLightState GetPedestrianLightState(RoadBaseAI.TrafficLightState vehicleLightState) {
#if TRACE
			Singleton<CodeProfiler>.instance.Start("CustomSegmentLight.GetPedestrianLightState");
#endif
			RoadBaseAI.TrafficLightState ret = RoadBaseAI.TrafficLightState.Green;
			switch (vehicleLightState) {
				case RoadBaseAI.TrafficLightState.Red:
				default:
					ret = RoadBaseAI.TrafficLightState.Green;
					break;
				case RoadBaseAI.TrafficLightState.Green:
					ret = RoadBaseAI.TrafficLightState.Red;
					break;
				case RoadBaseAI.TrafficLightState.RedToGreen:
					ret = RoadBaseAI.TrafficLightState.GreenToRed;
					break;
				case RoadBaseAI.TrafficLightState.GreenToRed:
					ret = RoadBaseAI.TrafficLightState.RedToGreen;
					break;
			}
#if TRACE
			Singleton<CodeProfiler>.instance.Stop("CustomSegmentLight.GetPedestrianLightState");
#endif
			return ret;
		}

		internal void MakeRedOrGreen() {
#if TRACE
			Singleton<CodeProfiler>.instance.Start("CustomSegmentLight.MakeRedOrGreen");
#endif
			if (LightLeft == RoadBaseAI.TrafficLightState.RedToGreen) {
				LightLeft = RoadBaseAI.TrafficLightState.Green;
			} else if (LightLeft == RoadBaseAI.TrafficLightState.GreenToRed) {
				LightLeft = RoadBaseAI.TrafficLightState.Red;
			}

			if (LightMain == RoadBaseAI.TrafficLightState.RedToGreen) {
				LightMain = RoadBaseAI.TrafficLightState.Green;
			} else if (LightMain == RoadBaseAI.TrafficLightState.GreenToRed) {
				LightMain = RoadBaseAI.TrafficLightState.Red;
			}

			if (LightRight == RoadBaseAI.TrafficLightState.RedToGreen) {
				LightRight = RoadBaseAI.TrafficLightState.Green;
			} else if (LightRight == RoadBaseAI.TrafficLightState.GreenToRed) {
				LightRight = RoadBaseAI.TrafficLightState.Red;
			}
#if TRACE
			Singleton<CodeProfiler>.instance.Stop("CustomSegmentLight.MakeRedOrGreen");
#endif
		}

		internal void MakeRed() {
            LightLeft = RoadBaseAI.TrafficLightState.Red;
			LightMain = RoadBaseAI.TrafficLightState.Red;
			LightRight = RoadBaseAI.TrafficLightState.Red;
		}
	}
}
