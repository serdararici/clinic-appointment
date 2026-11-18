import { useState, type FormEvent } from 'react'
import { readableMessage } from '../../api/errors'
import { Alert } from '../../components/Alert'
import { Button } from '../../components/Button'
import { Input } from '../../components/Input'
import { Modal } from '../../components/Modal'

interface NameFormModalProps {
  open: boolean
  title: string
  /** Label and values of the optional second field (a branch has an address); leave out for a name only. */
  addressLabel?: string
  initialName: string
  initialAddress?: string
  nameLabel: string
  nameRequiredMessage: string
  addressRequiredMessage?: string
  onClose: () => void
  /** Saves; throws an ApiError that is shown in the dialog. */
  onSubmit: (name: string, address: string) => Promise<void>
}

/**
 * A small form for organizations (a name) and branches (a name and an address). The form is only mounted while
 * the modal is open, so it always starts with the given values.
 */
export function NameFormModal(props: NameFormModalProps) {
  return (
    <Modal
      open={props.open}
      onClose={props.onClose}
      title={props.title}
      footer={
        <>
          <Button variant="secondary" onClick={props.onClose}>
            Vazgeç
          </Button>
          <Button type="submit" form="name-form">
            Kaydet
          </Button>
        </>
      }
    >
      {props.open ? <NameForm {...props} /> : null}
    </Modal>
  )
}

function NameForm({ nameLabel, addressLabel, initialName, initialAddress = '', nameRequiredMessage, addressRequiredMessage, onSubmit }: NameFormModalProps) {
  const [name, setName] = useState(initialName)
  const [address, setAddress] = useState(initialAddress)
  const [errors, setErrors] = useState<{ name?: string; address?: string }>({})
  const [apiError, setApiError] = useState<string | null>(null)

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    setApiError(null)
    const found: typeof errors = {}
    if (!name.trim()) found.name = nameRequiredMessage
    if (addressLabel && !address.trim()) found.address = addressRequiredMessage
    setErrors(found)
    if (Object.keys(found).length > 0) return
    try {
      await onSubmit(name.trim(), address.trim())
    } catch (caught) {
      setApiError(readableMessage(caught))
    }
  }

  return (
    <form id="name-form" onSubmit={submit} noValidate className="space-y-3">
      <Input label={nameLabel} value={name} onChange={(event) => setName(event.target.value)} error={errors.name} autoFocus />
      {addressLabel ? <Input label={addressLabel} value={address} onChange={(event) => setAddress(event.target.value)} error={errors.address} /> : null}
      {apiError ? <Alert tone="error">{apiError}</Alert> : null}
    </form>
  )
}
