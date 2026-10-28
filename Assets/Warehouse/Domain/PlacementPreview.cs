using System;
using System.Collections.Generic;

namespace Warehouse.Domain
{
    /// <summary>
    /// One-cell display-only prototype footprint, not an equipment-placement
    /// transaction. Bounds/footprint are P UX fixtures, not reference rules.
    /// </summary>
    public sealed class PlacementPreview
    {
        private readonly GhostFixtureSnapshot initial;
        private readonly HashSet<GridCell> occupied;
        private readonly int minX, maxX, minZ, maxZ;
        public string EquipmentTypeId => initial.EquipmentTypeId;
        public int X { get; private set; }
        public int Z { get; private set; }
        public int Floor { get; private set; }
        public int RotationQuarterTurns { get; private set; }
        public bool Flipped { get; private set; }
        public bool Active { get; private set; }
        public bool IsActive => Active;
        public bool IsValid { get; private set; }
        public bool Valid => IsValid;
        public string InvalidReason { get; private set; }
        public string ValidationReason => InvalidReason;

        public PlacementPreview(GhostFixtureSnapshot fixture, int minX = -6, int maxX = 6,
                                int minZ = -6, int maxZ = 6)
        {
            if (fixture == null) throw new ArgumentNullException(nameof(fixture));
            if (minX > maxX || minZ > maxZ) throw new ArgumentException("Invalid preview bounds.");
            initial = fixture;
            this.minX = minX; this.maxX = maxX; this.minZ = minZ; this.maxZ = maxZ;
            occupied = new HashSet<GridCell>(fixture.OccupiedCells);
            Reset();
        }

        public void MoveTo(int x, int z, int floor = 0)
        {
            X = x; Z = z; Floor = floor;
            Active = true;
            Validate();
        }

        public void Rotate()
        {
            if (!Active) return;
            RotationQuarterTurns = (RotationQuarterTurns + 1) % 4;
            Validate();
        }

        public void Flip()
        {
            if (!Active) return;
            Flipped = !Flipped;
            Validate();
        }

        public void Reset()
        {
            X = initial.InitialPosition.X; Z = initial.InitialPosition.Z; Floor = initial.InitialPosition.Floor;
            RotationQuarterTurns = initial.InitialRotationQuarterTurns;
            Flipped = initial.InitialFlipped;
            Active = true;
            Validate();
        }

        public void Cancel()
        {
            Reset();
            Active = false;
            Validate();
        }

        private void Validate()
        {
            IsValid = false;
            if (!Active) { InvalidReason = "プレビューを取り消しました。資金・設備は変更していません。"; return; }
            if (Floor != initial.InitialPosition.Floor || X < minX || X > maxX || Z < minZ || Z > maxZ)
            { InvalidReason = "範囲外：試作用グリッドの内側へ移動してください。"; return; }
            if (occupied.Contains(new GridCell(X, Z, Floor)))
            { InvalidReason = "使用中のセル：既設設備と重なっています。"; return; }
            IsValid = true;
            InvalidReason = "配置可能（表示のみ・本設置は後のSprint）";
        }
    }
}
