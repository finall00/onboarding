import { useState } from "react";
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  TextField,
  Box,
} from "@mui/material";

interface CreateLeadListDialogProps {
  isOpen: boolean;
  onClose: () => void;
  onSubmit: (data: { name: string; sourceUrl: string }) => Promise<void>;
}

export function CreateLeadListDialog({
  isOpen,
  onClose,
  onSubmit,
}: CreateLeadListDialogProps) {
  const [name, setName] = useState("");
  const [sourceUrl, setSourceUrl] = useState("");
  const [errors, setErrors] = useState<{ name?: string; sourceUrl?: string }>(
    {}
  );
  const [isSubmitting, setIsSubmitting] = useState(false);

  const validateUrl = (url: string): boolean => {
    try {
      new URL(url);
      return true;
    } catch {
      return false;
    }
  };

  const handleSubmit = async () => {
    const newErrors: { name?: string; sourceUrl?: string } = {};

    if (!name.trim()) {
      newErrors.name = "Name is required";
    } else if (name.length > 100) {
      newErrors.name = "Name must be 100 characters or less";
    }

    if (!sourceUrl.trim()) {
      newErrors.sourceUrl = "Source URL is required";
    } else if (!validateUrl(sourceUrl)) {
      newErrors.sourceUrl = "Please enter a valid URL";
    } else if (sourceUrl.length > 500) {
      newErrors.sourceUrl = "URL must be 500 characters or less";
    }

    if (Object.keys(newErrors).length > 0) {
      setErrors(newErrors);
      return;
    }

    setIsSubmitting(true);
    try {
  await onSubmit({ name, sourceUrl });
      setName("");
      setSourceUrl("");
      setErrors({});
      onClose();
    } catch (error) {
      console.error("Failed to create lead list:", error);
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleClose = () => {
    if (!isSubmitting) {
      setName("");
      setSourceUrl("");
      setErrors({});
      onClose();
    }
  };

  return (
    <Dialog open={isOpen} onClose={handleClose} maxWidth="sm" fullWidth>
      <DialogTitle>Create Lead List</DialogTitle>
      <DialogContent>
        <Box sx={{ display: "flex", flexDirection: "column", gap: 2, mt: 1 }}>
          <TextField
            label="Name"
            value={name}
            onChange={(e) => setName(e.target.value)}
            error={!!errors.name}
            helperText={errors.name}
            placeholder="e.g., Lista Setembro"
            disabled={isSubmitting}
            fullWidth
            autoFocus
          />
          <TextField
            label="Source URL"
            value={sourceUrl}
            onChange={(e) => setSourceUrl(e.target.value)}
            error={!!errors.sourceUrl}
            helperText={errors.sourceUrl}
            placeholder="https://coolsite.com/leads.csv"
            disabled={isSubmitting}
            fullWidth
          />
        </Box>
      </DialogContent>
      <DialogActions>
        <Button onClick={handleClose} disabled={isSubmitting}>
          Cancel
        </Button>
        <Button
          onClick={handleSubmit}
          variant="contained"
          disabled={isSubmitting}
        >
          {isSubmitting ? "Creating..." : "Create"}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
