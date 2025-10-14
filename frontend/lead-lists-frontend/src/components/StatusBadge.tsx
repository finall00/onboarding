import { Chip, type ChipProps } from "@mui/material";
import type { LeadListStatus } from "../model/leadlist";

interface StatusBadgeProps {
  status: LeadListStatus;
}

export function StatusBadge({ status }: StatusBadgeProps) {
  const getStatusColor = () => {
    switch (status) {
      case "Pending":
        return "default";
      case "Processing":
        return "info";
      case "Completed":
        return "success";
      case "Failed":
        return "error";
      default:
        return "default";
    }
  };

  return (
    <Chip
      label={status}
  color={getStatusColor() as ChipProps['color']}
      size="medium"
      sx={{ fontWeight: 500 }}
    />
  );
}
