using Robust.Shared.Serialization;

namespace Content.Shared.Arcade
{
    public static class SlotMachineMessages
    {
        [Serializable, NetSerializable]
        public sealed class SlotMachinePlayerActionMessage : BoundUserInterfaceMessage
        {
            public readonly SlotMachinePlayerAction PlayerAction;
            public readonly int BetAmount;

            public SlotMachinePlayerActionMessage(SlotMachinePlayerAction playerAction, int betAmount = 0)
            {
                PlayerAction = playerAction;
                BetAmount = betAmount;
            }
        }

        [Serializable, NetSerializable]
        public sealed class SlotMachineSpinResultMessage : BoundUserInterfaceMessage
        {
            public readonly SlotSymbol[] Symbols;
            public readonly int Payout;
            public readonly bool IsJackpot;

            public SlotMachineSpinResultMessage(SlotSymbol[] symbols, int payout, bool isJackpot = false)
            {
                Symbols = symbols;
                Payout = payout;
                IsJackpot = isJackpot;
            }
        }

        [Serializable, NetSerializable]
        public sealed class SlotMachineUpdateStateMessage : BoundUserInterfaceMessage
        {
            public readonly SlotMachineState State;
            public readonly int PlayerCredits;
            public readonly int LastBet;

            public SlotMachineUpdateStateMessage(SlotMachineState state, int playerCredits, int lastBet = 0)
            {
                State = state;
                PlayerCredits = playerCredits;
                LastBet = lastBet;
            }
        }

        [Serializable, NetSerializable]
        public enum SlotMachinePlayerAction
        {
            Spin,
            InsertMoney,
            CashOut
        }

        [Serializable, NetSerializable]
        public enum SlotMachineState
        {
            Idle,
            Spinning,
            ShowingResult,
            OutOfOrder,
            Hacked
        }

        [Serializable, NetSerializable]
        public enum SlotSymbol
        {
            Cherry,
            Lemon,
            Orange,
            Bell,
            Bar,
            Seven,
            Diamond,
            Jackpot
        }
    }
}