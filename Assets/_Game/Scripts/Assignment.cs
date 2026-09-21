namespace LifeSimulator
{
    public enum AssignmentState { Issued, Carrying, Delivered }

    // A plain object, not a MonoBehaviour or ScriptableObject, and deliberately not a generic
    // quest: it is exactly one warehouse order and nothing else.
    public sealed class Assignment
    {
        public Assignment(int orderNumber, WarehouseRack rack, WarehouseDropOff dropOff, WarehouseBox box)
        {
            OrderNumber = orderNumber;
            Rack = rack;
            DropOff = dropOff;
            Box = box;
            State = AssignmentState.Issued;
        }

        public int OrderNumber { get; }
        public WarehouseRack Rack { get; }
        public WarehouseDropOff DropOff { get; }
        public WarehouseBox Box { get; }
        public AssignmentState State { get; internal set; }

        public string Describe(int completed, int total)
        {
            string header = "ORDER #" + OrderNumber + "   (" + (completed + 1) + "/" + total + ")";
            return State == AssignmentState.Issued
                ? header + "\nPick up: " + Rack.DisplayName + "\nDeliver to: " + DropOff.DisplayName
                : header + "\nCarrying — deliver to: " + DropOff.DisplayName;
        }
    }
}
