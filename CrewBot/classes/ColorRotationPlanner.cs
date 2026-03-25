using System.Collections.Generic;
using System.Linq;

namespace CrewBot.Classes
{
    public static class ColorRotationPlanner
    {
        public static IReadOnlyList<ulong> GetAssignedColorRoleIds(
            IEnumerable<ulong> memberRoleIds,
            IEnumerable<ulong> availableColorRoleIds)
        {
            if (memberRoleIds == null || availableColorRoleIds == null)
            {
                return new List<ulong>();
            }

            HashSet<ulong> memberRoleIdSet = memberRoleIds.ToHashSet();
            return availableColorRoleIds
                .Where(id => memberRoleIdSet.Contains(id))
                .Distinct()
                .ToList();
        }
    }
}
