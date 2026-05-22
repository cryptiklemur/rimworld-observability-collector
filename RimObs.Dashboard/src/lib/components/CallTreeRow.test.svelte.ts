import { describe, it, expect } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/svelte';
import type { CallNode } from '../api';
import CallTreeRow from './CallTreeRow.svelte';

function node(over: Partial<CallNode> = {}): CallNode {
    return {
        id: 1,
        name: 'Root',
        call_count: 10,
        total_ns: 1000,
        is_other: false,
        children: [],
        ...over,
    };
}

describe('CallTreeRow', () => {
    it('renders nested children expanded by default', () => {
        const tree = node({
            children: [node({ id: 2, name: 'Child', total_ns: 400 })],
        });
        render(CallTreeRow, { node: tree, parentNs: 1000 });
        expect(screen.getByText('Root')).toBeInTheDocument();
        expect(screen.getByText('Child')).toBeInTheDocument();
    });

    it('collapses and re-expands children when the row is clicked', async () => {
        const tree = node({
            children: [node({ id: 2, name: 'Child', total_ns: 400 })],
        });
        render(CallTreeRow, { node: tree, parentNs: 1000 });
        const rootRow = screen.getByText('Root').closest('button');
        expect(rootRow).not.toBeNull();

        await fireEvent.click(rootRow!);
        expect(screen.queryByText('Child')).toBeNull();

        await fireEvent.click(rootRow!);
        expect(screen.getByText('Child')).toBeInTheDocument();
    });

    it('disables the toggle for a leaf node', () => {
        render(CallTreeRow, { node: node({ name: 'Leaf' }), parentNs: 1000 });
        expect(screen.getByText('Leaf').closest('button')).toBeDisabled();
    });
});
