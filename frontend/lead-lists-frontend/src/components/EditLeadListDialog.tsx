import { useState } from "react";
import { Dialog, DialogTitle, DialogContent } from "@mui/material";
import type { LeadList } from "../lib/leadlist";
import LeadListForm from "./LeadListForm";

interface EditLeadListDialogProps {
  isOpen: boolean;
  onClose: () => void;
  onSubmit: (data: { name: string; sourceUrl: string }) => Promise<void>;
  leadList: LeadList | null;
}

export function EditLeadListDialog({ isOpen, onClose, onSubmit, leadList }: EditLeadListDialogProps) {
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSubmit = async (values: { name: string; sourceUrl: string }) => {
    setIsSubmitting(true);
    try {
      await onSubmit(values);
      onClose();
    } catch (error) {
      const e = error as unknown;
      if (e && typeof e === "object" && "body" in (e as Record<string, unknown>)) {
        const body = (e as Record<string, unknown>).body as Record<string, unknown> | undefined;
        const apiErrors = body?.errors as Record<string, string[] | undefined> | undefined;
        if (apiErrors) {
          const mapped: Record<string, string> = {};
          if (Array.isArray(apiErrors.Name) && apiErrors.Name.length) mapped.name = apiErrors.Name.join(" ");
          if (Array.isArray(apiErrors.SourceUrl) && apiErrors.SourceUrl.length) mapped.sourceUrl = apiErrors.SourceUrl.join(" ");
          throw { apiErrors: mapped };
        }
      }
      throw error;
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Dialog open={isOpen} onClose={() => !isSubmitting && onClose()} maxWidth="sm" fullWidth>
      <DialogTitle>Edit Lead List</DialogTitle>
      <DialogContent>
        <LeadListForm
          initial={leadList ? { name: leadList.name, sourceUrl: leadList.sourceUrl } : {}}
          onSubmit={handleSubmit}
          onCancel={() => !isSubmitting && onClose()}
          submitting={isSubmitting}
          submitLabel="Save Changes"
        />
      </DialogContent>
    </Dialog>
  );
}
