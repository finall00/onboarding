import { useState } from "react";
import { Dialog, DialogTitle, DialogContent } from "@mui/material";
import LeadListForm from "./LeadListForm";

interface CreateLeadListDialogProps {
  isOpen: boolean;
  onClose: () => void;
  onSubmit: (data: { name: string; sourceUrl: string }) => Promise<void>;
}

export function CreateLeadListDialog({ isOpen, onClose, onSubmit }: CreateLeadListDialogProps) {
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSubmit = async (values: { name: string; sourceUrl: string }) => {
    setIsSubmitting(true);
    try {
      await onSubmit(values);
      onClose();
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Dialog open={isOpen} onClose={() => !isSubmitting && onClose()} maxWidth="sm" fullWidth>
      <DialogTitle>Create Lead List</DialogTitle>
      <DialogContent>
        <LeadListForm
          initial={{}}
          onSubmit={handleSubmit}
          onCancel={() => !isSubmitting && onClose()}
          submitting={isSubmitting}
          submitLabel="Create"
        />
      </DialogContent>
    </Dialog>
  );
}
