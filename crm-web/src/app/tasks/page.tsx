import { TasksClient } from '@/components/tasks/TasksClient';
import { Metadata } from 'next';

export const metadata: Metadata = {
  title: 'Tarefas | DIAX CRM',
  description: 'Gerencie suas tarefas e to-dos',
};

export default function TasksPage() {
  return (
    <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
      <TasksClient />
    </div>
  );
}
