using PlayerIOClient;

namespace BotBits.SendMessages
{
    /// <summary>
    ///     Sent to touch a hologram.
    /// </summary>
    /// <seealso cref="SendMessage{T}" />
    public sealed class TouchHologramSendMessage : SendMessage<TouchHologramSendMessage>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="TouchHologramSendMessage" /> class.
        /// </summary>
        /// <param name="x">The x-coordinate.</param>
        /// <param name="y">The y-coordinate.</param>
        public TouchHologramSendMessage(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }

        /// <summary>
        ///     Gets or sets the x-coordinate.
        /// </summary>
        /// <value>
        ///     The x-coordinate.
        /// </value>
        public int X { get; set; }

        /// <summary>
        ///     Gets or sets the y-coordinate.
        /// </summary>
        /// <value>
        ///     The y-coordinate.
        /// </value>
        public int Y { get; set; }

        /// <summary>
        ///     Gets the PlayerIO message representing the data in this <see cref="SendMessage{T}" />.
        /// </summary>
        /// <returns></returns>
        protected override Message GetMessage()
        {
            return Message.Create("hologramtouch", this.X, this.Y);
        }
    }
}