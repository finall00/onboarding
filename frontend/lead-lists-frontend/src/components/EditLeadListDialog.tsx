import { useState, useEffect } from "react";
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  TextField,
  Box,
} from "@mui/material";
import type { LeadList } from "../lib/leadlist";

interface EditLeadListDialogProps {
  isOpen: boolean;
  onClose: () => void;
  onSubmit: (data: { name: string; sourceUrl: string }) => Promise<void>;
  leadList: LeadList | null;
}

export function EditLeadListDialog({
  isOpen,
  onClose,
  onSubmit,
  leadList,
}: EditLeadListDialogProps) {
  const [name, setName] = useState("");
  const [sourceUrl, setSourceUrl] = useState("");
  const [errors, setErrors] = useState<{ name?: string; sourceUrl?: string }>(
    {}
  );
  const [isSubmitting, setIsSubmitting] = useState(false);

  useEffect(() => {
    if (leadList) {
      setName(leadList.name);
      setSourceUrl(leadList.sourceUrl);
    }
  }, [leadList]);

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
      setErrors({});
      onClose();
    } catch (error) {
        const e = error as unknown;
        if (e && typeof e === "object" && "body" in (e as Record<string, unknown>)) {
          const body = (e as Record<string, unknown>).body as Record<string, unknown> | undefined;
          const apiErrors = body?.errors as Record<string, string[] | undefined> | undefined;
          if (apiErrors) {
            const mapped: { name?: string; sourceUrl?: string } = {};
            if (Array.isArray(apiErrors.Name) && apiErrors.Name.length) mapped.name = apiErrors.Name.join(" ");
            if (Array.isArray(apiErrors.SourceUrl) && apiErrors.SourceUrl.length) mapped.sourceUrl = apiErrors.SourceUrl.join(" ");
            setErrors(mapped);
          }
        }
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleClose = () => {
    if (!isSubmitting) {
      setErrors({});
      onClose();
    }
  };

  return (
    <Dialog open={isOpen} onClose={handleClose} maxWidth="sm" fullWidth>
      <DialogTitle>Edit Lead List</DialogTitle>
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
            placeholder="https://example.com/leads.csv"
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
          {isSubmitting ? "Saving..." : "Save Changes"}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
