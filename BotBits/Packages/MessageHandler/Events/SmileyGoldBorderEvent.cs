﻿using PlayerIOClient;

namespace BotBits.Events
{
    /// <summary>
    ///     Occurs when someone enables or disables the gold smiley border.
    /// </summary>
    /// <seealso cref="PlayerEvent{T}" />
    [ReceiveEvent("smileyGoldBorder")]
    public sealed class SmileyGoldBorderEvent : PlayerEvent<MutedEvent>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="MutedEvent" /> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="client"></param>
        internal SmileyGoldBorderEvent(BotBitsClient client, Message message)
            : base(client, message)
        {
            this.Enabled = message.GetBoolean(1);
        }

        /// <summary>
        ///     Value indicating whether the player is wearing gold smiley border.
        /// </summary>
        /// <value><c>true</c> if muted; otherwise, <c>false</c>.</value>
        public bool Enabled { get; set; }
    }
}