﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BotBits.Events;
using BotBits.Nito;
using BotBits.SendMessages;

namespace BotBits
{
    public sealed class BlockChecker : EventListenerPackage<BlockChecker>, IDisposable
    {
        private readonly ManualResetEvent _finishResetEvent = new ManualResetEvent(true);
        private readonly RegisteredWaitHandle _registration;
        private readonly Deque<CheckHandle> _sentBlocks = new Deque<CheckHandle>(100);
        private readonly Dictionary<Point3D, CheckHandle> _sentLocations = new Dictionary<Point3D, CheckHandle>(100);
        private readonly AutoResetEvent _timeoutResetEvent = new AutoResetEvent(true);
        private MessageQueue<PlaceSendMessage> _messageQueue;

        [Obsolete("Invalid to use \"new\" on this class. Use the static .Of(BotBits) method instead.", true)]
        public BlockChecker()
        {
            this.InitializeFinish += this.BlockChecker_InitializeFinish;
            this._registration = this.RegisterSendTimeout();
        }

        void IDisposable.Dispose()
        {
            this._registration.Unregister(null);
            this._timeoutResetEvent.Dispose();
            this._finishResetEvent.Dispose();
        }

        private void BlockChecker_InitializeFinish(object sender, EventArgs e)
        {
            this._messageQueue = PlaceSendMessage.Of(this.BotBits);
        }

        private RegisteredWaitHandle RegisterSendTimeout()
        {
            return ThreadPool.RegisterWaitForSingleObject(this._timeoutResetEvent, (state, timedOut) =>
            {
                if (!timedOut) return;
                if (this._sentBlocks.Count == 0) return;
                if (this._messageQueue.Count != 0) return;

                lock (this._sentBlocks)
                {
                    this.RepairMissed();
                }
            }, null, 500, false);
        }

        public Task FinishChecksAsync()
        {
            // ReSharper disable once InconsistentlySynchronizedField
            return this._finishResetEvent.AsTask();
        }

        private void UpdateFinish()
        {
            if (this._sentBlocks.Count == 0 &&
                this._messageQueue.Count == 0) this._finishResetEvent.Set();
        }

        [EventListener]
        private void On(ForegroundPlaceEvent e)
        {
            this.Repair<ForegroundPlaceEvent, ForegroundBlock>(Layer.Foreground, e);
        }

        [EventListener]
        private void On(BackgroundPlaceEvent e)
        {
            this.Repair<BackgroundPlaceEvent, BackgroundBlock>(Layer.Background, e);
        }

        private void Repair<T, TBlock>(Layer layer, T e)
            where T : PlaceEvent<T, TBlock> where TBlock : struct
        {
            // Make sure the block was uploaded by this bot
            var p = e.New.Placer;
            if (p != null && p != Package<Players>.Of(this.BotBits).OwnPlayer) return;

            lock (this._sentBlocks)
            {
                var point = new Point3D(layer, e.X, e.Y);

                // Make sure we have sent a block at this location
                CheckHandle testB;
                if (!this._sentLocations.TryGetValue(point, out testB)) return;

                // If we have sent mutliple blocks at this position, wait until we receive the last one 
                if (testB.OverwrittenSends != 0)
                {
                    testB.OverwrittenSends--;
                }
                else if (WorldUtils.AreSame<T, TBlock>(testB.Message, e))
                {
                    // Reset the timeout
                    this._timeoutResetEvent.Set();

                    this.RepairMissed(point);
                }
            }
        }

        private void RepairMissed(Point3D? point = null)
        {
            while (this._sentBlocks.Count > 0)
            {
                var current = this._sentBlocks.RemoveFromFront();
                var currentPoint = GetPoint3D(current.Message);
                this._sentLocations.Remove(currentPoint);

                // If we arrived at the block we received, exit the checks
                if (currentPoint == point) break;

                this.SendMissed(current);
            }

            this.UpdateFinish();
        }

        private void SendMissed(CheckHandle handle)
        {
            MessageServices.EnableSkipsQueue(() => { handle.Message.SendIn(this.BotBits); });
        }

        [EventListener(GlobalPriority.AfterMost)]
        private void OnSendingPlace(SendingEvent<PlaceSendMessage> e)
        {
            var b = e.Message;
            var p = GetPoint3D(b);

            lock (this._sentBlocks)
            {
                if (!this.ShouldSend(b, p))
                {
                    e.Cancelled = true;

                    this.UpdateFinish();
                }
            }
        }

        [EventListener(GlobalPriority.AfterMost)]
        private void OnSendPlace(SendEvent<PlaceSendMessage> e)
        {
            var b = e.Message;
            var p = GetPoint3D(b);

            lock (this._sentBlocks)
            {
                this._timeoutResetEvent.Set();
                this._finishResetEvent.Reset();

                var overwrritenSends = 0;
                CheckHandle oldHandle;
                if (this._sentLocations.TryGetValue(p, out oldHandle))
                {
                    this._sentBlocks.Remove(oldHandle);
                    overwrritenSends = oldHandle.OverwrittenSends + 1;
                }

                var newHandle = new CheckHandle(b, overwrritenSends);
                this._sentBlocks.AddToBack(newHandle);
                this._sentLocations[p] = newHandle;
            }
        }

        private bool ShouldSend(PlaceSendMessage b, Point3D p)
        {
            if (b.SendCount > 10) return false;
            if (b.NoChecks) return true;
            if (!Actions.Of(this.BotBits).CanEdit) return false;

            var playerData = ConnectionManager.Of(this.BotBits).PlayerData;
            var blocks = Blocks.Of(this.BotBits);
            var isAdministrator = playerData.PlayerObject.IsAdministrator;

            if (!WorldUtils.IsPlaceable(b, blocks, !isAdministrator)) return false;
            if (!playerData.HasBlock(b.Id)) return false;

            CheckHandle handle;
            return !(this._sentLocations.TryGetValue(p, out handle)
                ? WorldUtils.AreSame(b, handle.Message)
                : WorldUtils.IsAlreadyPlaced(b, blocks));
        }

        private sealed class CheckHandle
        {
            public CheckHandle(PlaceSendMessage message, int overwrittenSends)
            {
                this.Message = message;
                this.OverwrittenSends = overwrittenSends;
            }

            public int OverwrittenSends { get; set; }
            public PlaceSendMessage Message { get; }
        }
        
        private static Point3D GetPoint3D(PlaceSendMessage placeSendMessage)
        {
            return new Point3D(placeSendMessage.Layer, placeSendMessage.X, placeSendMessage.Y);
        }
    }
}