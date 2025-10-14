import { useState, useEffect, useRef } from "react";
import { Box, TextField, Button, InputAdornment } from "@mui/material";

export interface LeadListFormValues {
  name: string;
  sourceUrl: string;
}

export interface LeadListFormProps {
  initial?: Partial<LeadListFormValues>;
  onSubmit: (values: LeadListFormValues) => Promise<void> | void;
  onCancel?: () => void;
  submitting?: boolean;
  submitLabel?: string;
  debounceMs?: number;
}

export default function LeadListForm({
  initial = {},
  onSubmit,
  onCancel,
  submitting = false,
  submitLabel = "Save",
  debounceMs = 800,
}: LeadListFormProps) {
  const [name, setName] = useState(initial.name ?? "");
  const [sourceUrl, setSourceUrl] = useState(initial.sourceUrl ?? "");
  const [errors, setErrors] = useState<{ name?: string; sourceUrl?: string }>({});
  const [emojiNotice, setEmojiNotice] = useState<string | null>(null);
  const [isNameFocused, setIsNameFocused] = useState(false);
  const [isNameHovered, setIsNameHovered] = useState(false);
  const [isNameSelected, setIsNameSelected] = useState(false);
  const lastSubmitRef = useRef<number>(0);

  useEffect(() => {
    setName(initial.name ?? "");
    setSourceUrl(initial.sourceUrl ?? "");
    setErrors({});
  }, [initial]);

  const removeEmojis = (s: string) => s.replace(/\p{Emoji}/gu, "").replace(/\uFE0F/g, "");

  const validateUrl = (url: string): boolean => {
    try {
      new URL(url);
      return true;
    } catch {
      return false;
    }
  };

  const handleSubmit = async () => {
    const now = Date.now();
    if (now - lastSubmitRef.current < (debounceMs ?? 800)) {
      return;
    }
    lastSubmitRef.current = now;
    const newErrors: { name?: string; sourceUrl?: string } = {};
    const trimmedName = name.trim();
    const trimmedUrl = sourceUrl.trim();

    if (!trimmedName) newErrors.name = "Name is required";
    else if (trimmedName.length > 100) newErrors.name = "Name must be 100 characters or less";

    if (!trimmedUrl) newErrors.sourceUrl = "Source URL is required";
    else if (!validateUrl(trimmedUrl)) newErrors.sourceUrl = "Please enter a valid URL";
    else if (trimmedUrl.length > 500) newErrors.sourceUrl = "URL must be 500 characters or less";

    if (Object.keys(newErrors).length) {
      setErrors(newErrors);
      return;
    }

    setErrors({});
    try {
      await onSubmit({ name: trimmedName, sourceUrl: trimmedUrl });
    } catch (err) {
      if (err && typeof err === "object" && "apiErrors" in (err as Record<string, unknown>)) {
        const api = (err as Record<string, unknown>).apiErrors as Record<string, string> | undefined;
        if (api) {
          setErrors({
            name: api.name ?? api.Name ?? undefined,
            sourceUrl: api.sourceUrl ?? api.SourceUrl ?? undefined,
          });
          return;
        }
      }
      throw err;
    }
  };

  return (
    <Box component="form" onSubmit={async (e) => { e.preventDefault(); await handleSubmit(); }} sx={{ display: "flex", flexDirection: "column", gap: 2, mt: 1 }}>
      <Box onMouseEnter={() => setIsNameHovered(true)} onMouseLeave={() => setIsNameHovered(false)}>
      <TextField
        label="Name"
        value={name}
        onChange={(e) => {
          const raw = e.target.value;
          const cleaned = removeEmojis(raw);
          if (cleaned.length !== raw.length) {
            setEmojiNotice("Emojis were removed from the name");
            setTimeout(() => setEmojiNotice(null), 2000);
          }
          setName(cleaned.slice(0, 100));
        }}
        onFocus={() => setIsNameFocused(true)}
        onBlur={() => setIsNameFocused(false)}
        inputProps={{
          onSelect: () => setIsNameSelected(true),
          onBlur: () => setIsNameSelected(false),
        }}
        error={!!errors.name}
        helperText={errors.name ?? emojiNotice}
        placeholder="e.g., List"
        disabled={submitting}
        fullWidth
        autoFocus
        InputProps={{
          endAdornment: (isNameFocused || isNameHovered || isNameSelected) ? (
            <InputAdornment position="end" sx={{ fontSize: 12, color: "text.secondary" }}>
              {`${name.length}/100`}
            </InputAdornment>
          ) : undefined,
        }}
      />
      </Box>

      <TextField
        label="Source URL"
        value={sourceUrl}
        onChange={(e) => setSourceUrl(e.target.value)}
        error={!!errors.sourceUrl}
        helperText={errors.sourceUrl}
        placeholder="https://example.com/leads.csv"
        disabled={submitting}
        fullWidth
      />

      <Box sx={{ display: "flex", gap: 1, justifyContent: "flex-end" }}>
        <Button type="button" onClick={onCancel} disabled={submitting}>
          Cancel
        </Button>
        <Button type="submit" variant="contained" disabled={submitting}>
          {submitting ? "Saving..." : submitLabel}
        </Button>
      </Box>
    </Box>
  );
}
