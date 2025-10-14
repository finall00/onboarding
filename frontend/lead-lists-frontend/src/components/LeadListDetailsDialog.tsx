import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  Box,
  Typography,
  Divider,
  Link,
  Alert,
} from "@mui/material";
import type { LeadList } from "../model/leadlist";
import { StatusBadge } from "./StatusBadge";

interface LeadListDetailsDialogProps {
  isOpen: boolean;
  onClose: () => void;
  leadList: LeadList | null;
}

export function LeadListDetailsDialog({
  isOpen,
  onClose,
  leadList,
}: LeadListDetailsDialogProps) {
  if (!leadList) return null;

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleString("en-US", {
      year: "numeric",
      month: "short",
      day: "numeric",
      hour: "2-digit",
      minute: "2-digit",
    });
  };

  return (
    <Dialog open={isOpen} onClose={onClose} maxWidth="md" fullWidth>
      <DialogTitle>Lead List Details</DialogTitle>
      <DialogContent>
        <Box sx={{ display: "flex", flexDirection: "column", gap: 3, mt: 1 }}>
          <Box>
            <Typography variant="subtitle2" color="text.secondary" gutterBottom>
              Name
            </Typography>
            <Typography variant="body1">{leadList.name}</Typography>
          </Box>

          <Box>
            <Typography variant="subtitle2" color="text.secondary" gutterBottom>
              Source URL
            </Typography>
            <Link
              href={leadList.sourceUrl}
              target="_blank"
              rel="noopener noreferrer"
              sx={{ wordBreak: "break-all" }}
              >
              {leadList.sourceUrl}
            </Link>
          </Box>

          <Box sx={{ display: "flex", gap: 3, flexWrap: "wrap" }}>
            <Box sx={{ flex: "1 1 200px" }}>
              <Typography
                variant="subtitle2"
                color="text.secondary"
                gutterBottom
              >
                Status
              </Typography>
              <StatusBadge status={leadList.status} />
            </Box>

            <Box sx={{ flex: "1 1 200px" }}>
              <Typography
                variant="subtitle2"
                color="text.secondary"
                gutterBottom
              >
                Processed Count
              </Typography>
              <Typography variant="body1">
                {leadList.processedCount.toLocaleString()}
              </Typography>
            </Box>
          </Box>

          {leadList.errorMessage && (
            <Alert severity="error">
              <Typography variant="subtitle2" gutterBottom>
                Error Message
              </Typography>
              <Typography variant="body2">{leadList.errorMessage}</Typography>
            </Alert>
          )}

          <Divider />

          <Box>
            <Typography variant="subtitle2" color="text.secondary" gutterBottom>
              Timestamps
            </Typography>
            <Box sx={{ display: "flex", gap: 2, flexWrap: "wrap", mt: 0.5 }}>
              <Box sx={{ flex: "1 1 200px" }}>
                <Typography variant="caption" color="text.secondary">
                  Created At
                </Typography>
                <Typography variant="body2">
                  {formatDate(leadList.createdAt)}
                </Typography>
              </Box>
              <Box sx={{ flex: "1 1 200px" }}>
                <Typography variant="caption" color="text.secondary">
                  Updated At
                </Typography>
                <Typography variant="body2">
                  {formatDate(leadList.updatedAt)}
                </Typography>
              </Box>
            </Box>
          </Box>

          <Box sx={{ display: "flex", gap: 2, flexWrap: "wrap", mt: 0.5 }}>
            <Box sx={{ flex: "1 1 200px" }}>
              <Typography
                variant="subtitle2"
                color="text.secondary"
                gutterBottom
              >
                LeadList ID
              </Typography>
              <Typography
                variant="body2"
                sx={{ fontFamily: "monospace", fontSize: "0.875rem" }}
              >
                {leadList.id}
              </Typography>
            </Box>
            <Box sx={{ flex: "1 1 200px" }}>
              <Typography
                variant="subtitle2"
                color="text.secondary"
                gutterBottom
              >
                Correlation ID
              </Typography>
              <Typography
                variant="body2"
                sx={{ fontFamily: "monospace", fontSize: "0.875rem" }}
              >
                {leadList.correlationId}
              </Typography>
            </Box>
          </Box>
        </Box>
      </DialogContent>
      <DialogActions>
        <Button  onClick={onClose}>Close</Button>
      </DialogActions>
    </Dialog>
  );
}
